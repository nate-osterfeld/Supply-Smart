using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;
using Xunit.Abstractions;

namespace SupplySmart.Tests;

public class StockTransferTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(5, HttpStatusCode.OK)]
    [InlineData(6, HttpStatusCode.BadRequest)]
    public async Task TransferCannotOverflowDestination(int quantity, HttpStatusCode expected)
    {
        using var app = new SettingsApp();
        var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, int.MaxValue - 5);
        var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 2, quantity });
        var stocks = await InventoryTestData.Quantities(app);
        output.WriteLine($"Status={(int)response.StatusCode}; source={stocks[0]}; destination={stocks[1]}");
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.OK ? new[] { 5, int.MaxValue } : new[] { 10, int.MaxValue - 5 }, stocks);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Empty(app.Email.Recipients);
    }

    [Fact]
    public async Task SplitKeepsTheOriginalClassificationAndValue()
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20, "DIFFERENT");
        await app.ReadDb(db => db.InventoryItems.Where(i => i.Id == 1).ExecuteUpdateAsync(set => set
            .SetProperty(i => i.Type, ItemType.Reusable)
            .SetProperty(i => i.Category, "Instruments")
            .SetProperty(i => i.UnitCost, 17.50m)));

        using var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 2, quantity = 3 });
        response.EnsureSuccessStatusCode();
        var split = await app.ReadDb(db => db.InventoryItems.SingleAsync(i => i.ContainerId == 2 && i.Sku == "SAME"));
        Assert.Equal(ItemType.Reusable, split.Type);
        Assert.Equal("Instruments", split.Category);
        Assert.Equal(17.50m, split.UnitCost);
        Assert.Equal(3, split.Quantity);
    }

    [Theory]
    [InlineData("Other supply", "", ItemType.Consumable, 0)]
    [InlineData("Sample", "Instruments", ItemType.Consumable, 0)]
    [InlineData("Sample", "", ItemType.Reusable, 0)]
    [InlineData("Sample", "", ItemType.Consumable, 5)]
    public async Task DistinctStockLinesSharingSkuAndLotStaySeparate(
        string name, string category, ItemType type, int unitCost)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        await app.ReadDb(db => db.InventoryItems.Where(i => i.Id == 2).ExecuteUpdateAsync(set => set
            .SetProperty(i => i.Name, name)
            .SetProperty(i => i.Category, category)
            .SetProperty(i => i.Type, type)
            .SetProperty(i => i.UnitCost, (decimal)unitCost)));

        using var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 2, quantity = 3 });
        response.EnsureSuccessStatusCode();
        Assert.Equal(new[] { 7, 20, 3 }, await InventoryTestData.Quantities(app));
    }

    [Fact]
    public async Task TransferCannotTargetAnotherOrganization()
    {
        using var app = new SettingsApp();
        var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 3, quantity = 5 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(new[] { 10, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
    }
}

internal static class InventoryTestData
{
    public static Task<int> Seed(SettingsApp app, int targetQuantity, string targetSku = "SAME") => app.ReadDb(async db =>
    {
        db.Locations.AddRange(new Location { Id = 1, OrganizationId = 1, Name = "Own clinic" },
            new Location { Id = 2, OrganizationId = 2, Name = "Other clinic" });
        db.Rooms.AddRange(new Room { Id = 1, OrganizationId = 1, LocationId = 1, Name = "Own room" },
            new Room { Id = 2, OrganizationId = 2, LocationId = 2, Name = "Other room" });
        db.Containers.AddRange(new Container { Id = 1, OrganizationId = 1, RoomId = 1, Name = "Source" },
            new Container { Id = 2, OrganizationId = 1, RoomId = 1, Name = "Target" },
            new Container { Id = 3, OrganizationId = 2, RoomId = 2, Name = "Foreign" });
        db.InventoryItems.AddRange(
            new InventoryItem { Id = 1, OrganizationId = 1, ContainerId = 1, Name = "Sample", Sku = "SAME", Quantity = 10 },
            new InventoryItem { Id = 2, OrganizationId = 1, ContainerId = 2, Name = "Sample", Sku = targetSku, Quantity = targetQuantity });
        return await db.SaveChangesAsync();
    });

    public static Task<int[]> Quantities(SettingsApp app) => app.ReadDb(db =>
        db.InventoryItems.OrderBy(i => i.Id).Select(i => i.Quantity).ToArrayAsync());
}
