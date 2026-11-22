using System.Net.Http.Json;
using System.Text.Json;

namespace SupplySmart.Tests;

public class StockExpirationTests
{
    [Theory]
    [InlineData(0, "2030-01-15T23:46:51.1234567Z", true)]
    [InlineData(0, null, false)]
    [InlineData(1, "2030-01-15T23:46:51.1234567Z", false)]
    public async Task ReceiptAndBothEditorsReceiveTheOriginalExpiration(int type, string? expiration, bool hasExpiry)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        using var result = await staff.PostAsJsonAsync("/api/locations/containers/1/items",
            new { name = "Test supply", sku = "EXP-1", quantity = 8, type, expirationDate = expiration });
        result.EnsureSuccessStatusCode();
        var receipt = await result.Content.ReadFromJsonAsync<JsonElement>();
        var id = receipt.GetProperty("id").GetInt32();
        var expected = hasExpiry ? new DateTime(2030, 1, 15, 23, 46, 51, DateTimeKind.Utc).AddTicks(1234567) : (DateTime?)null;

        AssertExpiration(receipt, expected);
        foreach (var route in new[] { "/api/catalog", "/api/locations/items?containerId=1" })
        {
            var rows = await staff.GetFromJsonAsync<JsonElement[]>(route);
            AssertExpiration(rows!.Single(item => item.GetProperty("id").GetInt32() == id), expected);
        }
    }

    private static void AssertExpiration(JsonElement row, DateTime? expected)
    {
        Assert.Equal(expected?.ToString("MMM dd, yyyy") ?? "N/A", row.GetProperty("expirationDate").GetString());
        var timestamp = row.GetProperty("expirationTimestamp");
        if (expected.HasValue)
            Assert.Equal(expected.Value.Ticks, timestamp.GetDateTime().Ticks);
        else
            Assert.Equal(JsonValueKind.Null, timestamp.ValueKind);
    }
}
