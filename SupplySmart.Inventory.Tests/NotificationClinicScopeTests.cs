using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.DbContexts;

namespace SupplySmart.Tests;

public class NotificationClinicScopeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreviewAndAutomaticMailUseOnlyStockInTheClinicsStorage(bool mixedOwnership)
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        await InventoryTestData.Seed(app, 3);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SupplySmartDbContext>();
        var items = await db.InventoryItems.OrderBy(i => i.Id).ToListAsync();
        if (mixedOwnership)
            foreach (var item in items) item.ContainerId = 3;
        items[0].Quantity = 0;
        await db.SaveChangesAsync();

        using var response = await admin.PostAsJsonAsync("/api/settings/notifications/test", new
        {
            triggerType = NotificationService.TriggerLowStock,
            templateMarkdown = "{{item_name}} / {{supply_room}} / {{location_name}}",
            additionalRecipients = new[] { "supplies@example.test" }
        });
        response.EnsureSuccessStatusCode();
        var preview = Assert.Single(app.Email.AlertBodies);
        Assert.DoesNotContain("Other clinic", preview);
        Assert.DoesNotContain("Other room", preview);
        Assert.Contains(mixedOwnership ? "Primary Clinic Hub" : "Own clinic", preview);

        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await service.CheckAndTriggerItemStockAlertAsync(1, 3, items[0]);
        Assert.Equal(mixedOwnership ? 1 : 2, app.Email.AlertBodies.Count);
        if (!mixedOwnership) Assert.Contains("Own room", app.Email.AlertBodies[1]);
    }
}
