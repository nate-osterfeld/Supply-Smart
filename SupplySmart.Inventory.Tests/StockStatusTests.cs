using System.Net.Http.Json;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class StockStatusTests
{
    [Theory]
    [InlineData(0, null, "Out of Stock", null, "Out of Stock")]
    [InlineData(0, -2, "Out of Stock", "Expired", "Out of Stock")]
    [InlineData(2, null, "Low Stock", null, "Low Stock")]
    [InlineData(2, -2, "Low Stock", "Expired", "Expired")]
    [InlineData(2, 10, "Low Stock", "Expiring Soon", "Expiring Soon")]
    [InlineData(8, 60, "Safe", null, "In Stock")]
    public async Task NewStockHasConsistentAvailabilityAcrossViews(
        int quantity, int? daysToExpiry, string firstStatus, string? secondStatus, string catalogStatus)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        using var response = await staff.PostAsJsonAsync("/api/locations/containers/1/items",
            new CreateItemDto
            {
                Name = "New supply", Sku = "NEW", Quantity = quantity, MinimumThreshold = 2,
                ExpirationDate = daysToExpiry.HasValue ? DateTime.UtcNow.Date.AddDays(daysToExpiry.Value) : null
            });
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<InventoryItemDto>())!;
        var bin = await staff.GetFromJsonAsync<List<InventoryItemDto>>("/api/locations/items?containerId=1");
        var catalog = await staff.GetFromJsonAsync<List<MasterCatalogItemDto>>("/api/catalog");

        var expected = secondStatus is null ? new[] { firstStatus } : new[] { firstStatus, secondStatus };
        Assert.Equal(expected, created.Statuses);
        Assert.Equal(expected, bin!.Single(i => i.Id == created.Id).Statuses);
        Assert.Equal(catalogStatus, catalog!.Single(i => i.Id == created.Id).Status);
    }
}
