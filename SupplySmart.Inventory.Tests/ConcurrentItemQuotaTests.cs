using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupplySmart.Integrations.DbContexts;

namespace SupplySmart.Tests;

public class ConcurrentItemQuotaTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task TwoStockAllocationsShareTheLastClinicSlot(bool splitFirst, bool splitSecond)
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var connection = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(directory.FullName, "stock.db"),
                Pooling = false
            };
            using var app = new SettingsApp(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<SupplySmartDbContext>>();
                services.AddDbContext<SupplySmartDbContext>(options => options.UseSqlite(connection.ToString()));
            });
            using var first = await app.SignIn(3);
            using var second = await app.SignIn(4);
            await InventoryTestData.Seed(app, 20, "SECOND");
            await app.ReadDb(db => db.Organizations.Where(o => o.Id == 1)
                .ExecuteUpdateAsync(set => set.SetProperty(o => o.MaxItemsQuota, 3)));

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<HttpResponseMessage> Allocate(HttpClient client, bool split, int source, int destination)
            {
                await start.Task;
                return split
                    ? await client.PostAsJsonAsync($"/api/locations/items/{source}/move", new { targetContainerId = destination, quantity = 1 })
                    : await client.PostAsJsonAsync($"/api/locations/containers/{destination}/items", new { name = "New supply", sku = $"NEW-{source}", quantity = 1, minimumThreshold = 0 });
            }

            var firstRequest = Allocate(first, splitFirst, 1, 2);
            var secondRequest = Allocate(second, splitSecond, 2, 1);
            start.SetResult();
            await Task.WhenAll(firstRequest, secondRequest);
            using var firstResult = await firstRequest;
            using var secondResult = await secondRequest;
            var results = new[] { firstResult, secondResult };
            Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
            var rejected = Assert.Single(results, r => r.StatusCode == HttpStatusCode.BadRequest);
            Assert.Contains("item limit (3 items)", await rejected.Content.ReadAsStringAsync());
            var quantities = await InventoryTestData.Quantities(app);
            Assert.Equal(3, quantities.Length);
            bool winnerSplit = firstResult.IsSuccessStatusCode ? splitFirst : splitSecond;
            Assert.Equal(winnerSplit ? 30 : 31, quantities.Sum());
            Assert.Equal(1, await app.ReadDb(db => db.AuditLogs.CountAsync()));
            Assert.Empty(app.Email.Recipients);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
