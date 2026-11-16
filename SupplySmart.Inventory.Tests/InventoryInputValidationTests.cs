using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace SupplySmart.Tests;

public class InventoryInputValidationTests
{
    public static TheoryData<bool, string, object> InvalidInputs => new()
    {
        { false, "quantity", -1 }, { true, "quantity", int.MinValue },
        { false, "minimumThreshold", -1 }, { true, "minimumThreshold", -1 },
        { false, "unitCost", -0.01m }, { true, "unitCost", -1m },
        { false, "unitCost", 10000000000000000m }, { true, "unitCost", decimal.MaxValue },
        { false, "type", -1 }, { true, "type", 2 }
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public async Task InvalidCreateAndUpdateRequestsLeaveStockAndHistoryUntouched(bool update, string field, object value)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        var input = ValidInput();
        input[field] = value;

        var response = update
            ? await staff.PutAsJsonAsync("/api/locations/items/1", input)
            : await staff.PostAsJsonAsync("/api/locations/containers/1/items", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(new[] { 10, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Equal(0, await app.ReadDb(db => db.InventoryTransactions.CountAsync()));
        Assert.Empty(app.Email.Recipients);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task TransferCannotInstallANegativeThreshold(int quantity)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20, "DIFFERENT");
        var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 2, quantity, minimumThreshold = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(new[] { 10, 20 }, await InventoryTestData.Quantities(app));
        Assert.Equal(1, await app.ReadDb(async db => (await db.InventoryItems.SingleAsync(i => i.Id == 1)).ContainerId));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Empty(app.Email.Recipients);
    }

    [Theory]
    [InlineData(false, 0, 0)]
    [InlineData(true, 0, 1)]
    [InlineData(false, int.MaxValue, 1)]
    [InlineData(true, int.MaxValue, 0)]
    public async Task NonnegativeBoundaryValuesAndBothItemTypesRemainSupported(bool update, int quantity, int type)
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20);
        var input = ValidInput();
        input["quantity"] = quantity;
        input["minimumThreshold"] = 0;
        input["unitCost"] = 0m;
        input["type"] = type;
        var response = update
            ? await staff.PutAsJsonAsync("/api/locations/items/1", input)
            : await staff.PostAsJsonAsync("/api/locations/containers/1/items", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantity, await app.ReadDb(async db => (await db.InventoryItems.SingleAsync(i => i.Name == "Validated item")).Quantity));
    }

    private static Dictionary<string, object> ValidInput() => new()
    {
        ["name"] = "Validated item", ["sku"] = "VALID", ["quantity"] = 10,
        ["minimumThreshold"] = 5, ["unitCost"] = 1.25m, ["type"] = 0
    };
}
