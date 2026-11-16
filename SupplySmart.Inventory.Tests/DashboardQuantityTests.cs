using System.Net;
using System.Net.Http.Json;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class DashboardQuantityTests
{
    [Theory]
    [InlineData(true, false, int.MaxValue)]
    [InlineData(false, true, int.MaxValue)]
    [InlineData(true, true, 15)]
    public async Task StaffStockEditsCannotOverflowTheClinicsDashboard(bool expiring, bool lowStock, int quantity)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        foreach (var itemId in new[] { 1, 2 })
        {
            var update = await staff.PutAsJsonAsync($"/api/locations/items/{itemId}", new
            {
                name = $"Stock {itemId}", sku = $"STOCK-{itemId}", quantity,
                minimumThreshold = lowStock ? int.MaxValue : 0, unitCost = 1.50m,
                expirationDate = expiring ? DateTime.UtcNow.AddDays(7) : (DateTime?)null,
                type = 0
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        }

        using var otherClinic = await app.SignIn(5);
        var other = await otherClinic.GetFromJsonAsync<DashboardMetricsDto>("/api/dashboard/metrics");
        Assert.NotNull(other);
        Assert.Equal(0, other.ExpiringSoonCount);
        Assert.Equal(0, other.ActiveLowStockAlertsCount);

        using var admin = await app.SignIn(1);
        var response = await admin.GetAsync("/api/dashboard/metrics?locationId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        Assert.NotNull(metrics);
        Assert.Equal(expiring ? 2L * quantity : 0, metrics.ExpiringSoonCount);
        Assert.Equal(lowStock ? 2L * quantity : 0, metrics.ActiveLowStockAlertsCount);
        Assert.Equal(3m * quantity, metrics.TotalInventoryValue);
    }
}
