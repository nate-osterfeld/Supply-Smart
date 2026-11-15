using Microsoft.EntityFrameworkCore;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Forecast.Tests;

public class ForecastArithmeticTests
{
    [Theory]
    [InlineData(7, 700_000_000L)]
    [InlineData(14, 1_400_000_000L)]
    [InlineData(30, 3_000_000_000L)]
    public async Task RepeatedLargeCheckoutsProduceForecastForEveryHorizon(int days, long demand)
    {
        await using var db = await CreateDatabase();
        // Two legitimate int-sized movements must not overflow their aggregate.
        db.InventoryTransactions.AddRange(Checkout(1, -1_500_000_000), Checkout(1, -1_500_000_000));
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, days);
        var item = Assert.Single(forecast.Items);
        Assert.Equal(demand, item.PredictedDemand);
        Assert.Equal(demand, forecast.TotalPredictedDemand);
        Assert.Equal(-demand, item.ProjectedQuantity);
        Assert.Equal(demand + 5, item.RecommendedOrderQuantity);
        Assert.Equal("Critical", item.RiskLevel);
        Assert.Equal(1, forecast.SuggestedOrders);
    }

    [Fact]
    public async Task LargeThresholdDoesNotWrapOrderQuantityToZero()
    {
        await using var db = await CreateDatabase();
        (await db.InventoryItems.SingleAsync()).MinimumThreshold = 2_000_000_000;
        db.InventoryTransactions.Add(Checkout(1, -1_500_000_000));
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, 7);
        var item = Assert.Single(forecast.Items);
        Assert.Equal(350_000_000L, item.PredictedDemand);
        Assert.Equal(2_350_000_000L, item.RecommendedOrderQuantity);
        Assert.Equal(1, forecast.SuggestedOrders);
    }

    [Fact]
    public async Task SummarySupportsDemandAboveIntLimitAcrossItems()
    {
        await using var db = await CreateDatabase();
        db.InventoryItems.Add(new InventoryItem
        {
            Id = 2, OrganizationId = 1, ContainerId = 1, Name = "Second item", Sku = "SECOND"
        });
        db.InventoryTransactions.AddRange(Checkout(1, -1_500_000_000), Checkout(2, -1_500_000_000));
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, 30);
        Assert.Equal(2, forecast.Items.Count);
        Assert.All(forecast.Items, item => Assert.Equal(1_500_000_000L, item.PredictedDemand));
        Assert.Equal(3_000_000_000L, forecast.TotalPredictedDemand);
    }

    [Fact]
    public async Task MinimumSignedMovementIsConvertedBeforeTakingAbsoluteValue()
    {
        await using var db = await CreateDatabase();
        db.InventoryTransactions.Add(Checkout(1, int.MinValue));
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, 30);
        Assert.Equal(2_147_483_648L, forecast.TotalPredictedDemand);
    }

    [Theory]
    [InlineData(7, 8L)]
    [InlineData(14, 15L)]
    public async Task OrdinaryUsageRoundsUpFractionalUnits(int days, long demand)
    {
        await using var db = await CreateDatabase();
        (await db.InventoryItems.SingleAsync()).Quantity = 100;
        db.InventoryTransactions.Add(Checkout(1, -31));
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, days);
        var item = Assert.Single(forecast.Items);
        Assert.Equal(demand, item.PredictedDemand);
        Assert.Equal(100 - demand, item.ProjectedQuantity);
        Assert.Equal(0L, item.RecommendedOrderQuantity);
        Assert.Equal("Low", item.RiskLevel);
        Assert.Equal(0, forecast.ItemsAtRisk);
    }

    [Fact]
    public async Task OldHistoryAndRestocksDoNotCreateDemand()
    {
        await using var db = await CreateDatabase();
        var old = Checkout(1, -100);
        old.Timestamp = DateTime.UtcNow.AddDays(-31);
        var restock = Checkout(1, 100);
        restock.TransactionType = "restock";
        db.InventoryTransactions.AddRange(old, restock);
        await db.SaveChangesAsync();

        var forecast = await new ForecastService(db).GetForecastAsync(1, 7);
        Assert.Equal(0L, forecast.TotalPredictedDemand);
        Assert.Equal("Insufficient checkout history", Assert.Single(forecast.Items).ForecastMethod);
    }

    private static InventoryTransaction Checkout(int itemId, int change) => new()
    {
        OrganizationId = 1, InventoryItemId = itemId, TransactionType = "checkout",
        QuantityChange = change, Timestamp = DateTime.UtcNow,
        ItemName = "Test item", Sku = "TEST", UserEmail = "test@example.test"
    };

    private static async Task<SupplySmartDbContext> CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<SupplySmartDbContext>()
            .UseSqlite("Data Source=:memory:").Options;
        var db = new SupplySmartDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        db.Organizations.Add(new Organization { Id = 1, Name = "Test clinic" });
        db.Locations.Add(new Location { Id = 1, OrganizationId = 1, Name = "Test location" });
        db.Rooms.Add(new Room { Id = 1, OrganizationId = 1, LocationId = 1, Name = "Test room" });
        db.Containers.Add(new Container { Id = 1, OrganizationId = 1, RoomId = 1, Name = "Test container" });
        db.InventoryItems.Add(new InventoryItem
        {
            Id = 1, OrganizationId = 1, ContainerId = 1, Name = "Test item", Sku = "TEST",
            Quantity = 0, MinimumThreshold = 5
        });
        await db.SaveChangesAsync();
        return db;
    }
}
