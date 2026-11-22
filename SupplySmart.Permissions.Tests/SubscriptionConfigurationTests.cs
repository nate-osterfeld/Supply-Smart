using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SupplySmart.Domain.Services;

namespace SupplySmart.Tests;

public class SubscriptionConfigurationTests
{
    [Theory]
    [InlineData("Starter", false)]
    [InlineData("Professional", false)]
    [InlineData("Enterprise", false)]
    [InlineData("Starter", true)]
    [InlineData("Professional", true)]
    [InlineData("Enterprise", true)]
    public async Task IncompleteBillingConfigurationCannotGrantAPaidPlan(string plan, bool hasSecret)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StripeSettings:SecretKey"] = hasSecret ? "sk_test_local_only_not_a_real_key" : "",
            [$"StripeSettings:{plan}PriceId"] = hasSecret ? "" : "price_local_only"
        }).Build();

        await app.ReadDb(async db =>
        {
            var service = new SubscriptionService(db, configuration);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSubscriptionTierAsync(1, plan, "user1@example.test"));
            return 0;
        });

        await app.ReadDb(async db =>
        {
            var org = await db.Organizations.SingleAsync(o => o.Id == 1);
            Assert.Equal("Free", org.SubscriptionPlan);
            Assert.Equal(2, org.MaxRoomsQuota);
            Assert.Equal(50, org.MaxItemsQuota);
            Assert.Null(org.StripeCustomerId);
            Assert.Empty(await db.AuditLogs.ToListAsync());
            return 0;
        });
    }

    [Fact]
    public async Task FreePlanRemainsAvailableWithoutStripeConfiguration()
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        await app.ReadDb(async db =>
        {
            var service = new SubscriptionService(db, new ConfigurationBuilder().Build());
            var result = await service.UpdateSubscriptionTierAsync(1, "Free", "user1@example.test");
            Assert.True(result.UpdatedImmediately);
            Assert.Null(result.CheckoutUrl);
            Assert.Equal("Free", (await db.Organizations.SingleAsync(o => o.Id == 1)).SubscriptionPlan);
            return 0;
        });
    }
}
