using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace SupplySmart.Tests;

public class TransferQuotaTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("DIFFERENT", 2, 5, HttpStatusCode.BadRequest, 2)]
    [InlineData("DIFFERENT", 3, 5, HttpStatusCode.OK, 3)]
    [InlineData("SAME", 2, 5, HttpStatusCode.OK, 2)]
    [InlineData("DIFFERENT", 2, 10, HttpStatusCode.OK, 2)]
    public async Task OnlyTransfersCreatingNewRowsConsumeItemQuota(
        string targetSku, int quota, int quantity, HttpStatusCode expected, int expectedRows)
    {
        using var app = new SettingsApp();
        var staff = await app.SignIn(3);
        await InventoryTestData.Seed(app, 20, targetSku);
        await app.ReadDb(async db =>
        {
            (await db.Organizations.SingleAsync(o => o.Id == 1)).MaxItemsQuota = quota;
            return await db.SaveChangesAsync();
        });

        var response = await staff.PostAsJsonAsync("/api/locations/items/1/move",
            new { targetContainerId = 2, quantity });
        var count = await app.ReadDb(db => db.InventoryItems.CountAsync(i => i.OrganizationId == 1));
        output.WriteLine($"Status={(int)response.StatusCode}; quota={quota}; rows={count}");
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expectedRows, count);
        Assert.Equal(30, (await InventoryTestData.Quantities(app)).Sum());
        if (expected != HttpStatusCode.OK)
        {
            Assert.Equal(new[] { 10, 20 }, await InventoryTestData.Quantities(app));
            Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
            Assert.Empty(app.Email.Recipients);
        }
    }
}
