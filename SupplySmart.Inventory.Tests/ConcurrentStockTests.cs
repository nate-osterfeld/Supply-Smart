using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class ConcurrentStockTests
{
    [Fact]
    public async Task TwoOldSnapshotsCannotRecordTheSameCheckoutTwice()
    {
        using var app = new SettingsApp();
        app.NewClient();
        await InventoryTestData.Seed(app, 20);
        using var firstScope = app.Services.CreateScope();
        using var secondScope = app.Services.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<SupplySmartDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<SupplySmartDbContext>();
        await firstDb.InventoryItems.SingleAsync(i => i.Id == 1);
        await secondDb.InventoryItems.SingleAsync(i => i.Id == 1);
        var dto = new UpdateItemDto { Name = "Sample", Sku = "SAME", Quantity = 9 };

        Assert.True(await firstScope.ServiceProvider.GetRequiredService<ILocationService>()
            .UpdateItemAsync(1, 1, dto, "first@example.test"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            secondScope.ServiceProvider.GetRequiredService<ILocationService>()
                .UpdateItemAsync(1, 1, dto, "second@example.test"));

        Assert.Equal(new[] { 9, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(1, await app.ReadDb(db => db.InventoryTransactions.CountAsync()));
        Assert.Equal(-1, await app.ReadDb(db => db.InventoryTransactions.Select(t => t.QuantityChange).SingleAsync()));
        Assert.Equal(1, await app.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("unchanged-update")]
    [InlineData("move")]
    [InlineData("delete")]
    public async Task StaleApiMutationReturnsConflictWithoutPartialWrites(string operation)
    {
        using var app = new SettingsApp(services =>
        {
            services.RemoveAll<ILocationService>();
            services.AddScoped<ILocationService>(provider =>
            {
                var db = provider.GetRequiredService<SupplySmartDbContext>();
                db.InventoryItems.Single(i => i.Id == 1); // Request's old snapshot: quantity 10.
                using var competing = new SupplySmartDbContext(provider.GetRequiredService<DbContextOptions<SupplySmartDbContext>>());
                competing.InventoryItems.Where(i => i.Id == 1).ExecuteUpdate(set => set.SetProperty(i => i.Quantity, 9));
                return new LocationService(db, provider.GetRequiredService<INotificationService>());
            });
        });
        var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        await app.ReadDb(db => db.InventoryItems.Where(i => i.Id == 1)
            .ExecuteUpdateAsync(set => set.SetProperty(i => i.Category, "General")));

        var response = operation switch
        {
            "update" => await staff.PutAsJsonAsync("/api/locations/items/1", new UpdateItemDto { Name = "Sample", Sku = "SAME", Quantity = 8 }),
            "unchanged-update" => await staff.PutAsJsonAsync("/api/locations/items/1", new UpdateItemDto
                { Name = "Sample", Sku = "SAME", Quantity = 10, MinimumThreshold = 0, Category = "General" }),
            "move" => await staff.PostAsJsonAsync("/api/locations/items/1/move", new { targetContainerId = 2, quantity = 2 }),
            "delete" => await staff.DeleteAsync("/api/locations/items/1"),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(new[] { 9, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Equal(0, await app.ReadDb(db => db.InventoryTransactions.CountAsync()));
        Assert.Empty(app.Email.Recipients);
    }

    [Fact]
    public async Task UnchangedRequestWithoutCompetingWriteStillSucceeds()
    {
        using var app = new SettingsApp();
        var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        await app.ReadDb(db => db.InventoryItems.Where(i => i.Id == 1)
            .ExecuteUpdateAsync(set => set.SetProperty(i => i.Category, "General")));

        var response = await staff.PutAsJsonAsync("/api/locations/items/1", new UpdateItemDto
            { Name = "Sample", Sku = "SAME", Quantity = 10, MinimumThreshold = 0, Category = "General" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { 10, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(0, await app.ReadDb(db => db.InventoryTransactions.CountAsync()));
        Assert.Equal(1, await app.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Fact]
    public void SqlServerModelMatchesMigrationSnapshotWithoutConnecting()
    {
        using var db = new SupplySmartDbContext(new DbContextOptionsBuilder<SupplySmartDbContext>().UseSqlServer().Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
