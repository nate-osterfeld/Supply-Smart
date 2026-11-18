using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class StripeCheckoutTests
{
    private const string SessionId = "cs_fixture";
    private const string Path = "checkout/sessions/" + SessionId;

    private static object Checkout(object subscription, string payment = "paid", string customer = "cus_first",
        string organization = "1", string status = "complete", string mode = "subscription", string? previous = null)
    {
        var metadata = new Dictionary<string, string> { ["OrganizationId"] = organization, ["TargetPlan"] = "Enterprise" };
        if (previous is not null) metadata["PreviousSubscriptionId"] = previous;
        return new { id = SessionId, @object = "checkout.session", customer, mode, status, payment_status = payment, subscription, metadata };
    }

    private static Task<HttpResponseMessage> Complete(StripeBillingFixture billing, string payment = "paid",
        string eventType = "checkout.session.completed") => billing.Deliver(eventType, Checkout("sub_checkout", payment));

    private static async Task AssertPlan(StripeBillingFixture billing, string plan, int audits, string? subId)
    {
        await billing.App.ReadDb(async db =>
        {
            var org = await db.Organizations.SingleAsync(o => o.Id == 1);
            Assert.Equal(plan, org.SubscriptionPlan);
            Assert.Equal(subId, org.StripeSubscriptionId);
            Assert.Equal(plan switch { "Free" => (2, 50), "Starter" => (5, 250), _ => (15, 1500) },
                (org.MaxRoomsQuota, org.MaxItemsQuota));
            Assert.Equal(audits, await db.AuditLogs.CountAsync());
            Assert.Equal("Free", (await db.Organizations.SingleAsync(o => o.Id == 2)).SubscriptionPlan);
            return 0;
        });
    }

    [Fact]
    public async Task DelayedPaymentActivatesOnlyAfterSettlementAndOnlyOnce()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic(null);
        var active = StripeBillingFixture.Subscription("sub_checkout");
        billing.StripeResource(Path, Checkout(active, "unpaid"));
        using var pending = await Complete(billing, "unpaid");
        pending.EnsureSuccessStatusCode();
        await AssertPlan(billing, "Free", 0, null);

        billing.StripeResource(Path, Checkout(active));
        using var settled = await Complete(billing, eventType: "checkout.session.async_payment_succeeded");
        settled.EnsureSuccessStatusCode();
        await AssertPlan(billing, "Starter", 1, "sub_checkout");

        using var duplicate = await Complete(billing);
        duplicate.EnsureSuccessStatusCode();
        using var admin = await billing.App.SignIn(1);
        using var browserReturn = await admin.PostAsJsonAsync("/api/settings/subscription/verify-session", new { sessionId = SessionId });
        browserReturn.EnsureSuccessStatusCode();
        Assert.Equal("Starter", (await browserReturn.Content.ReadFromJsonAsync<SubscriptionDetailsDto>())!.PlanName);
        await AssertPlan(billing, "Starter", 1, "sub_checkout");
    }

    [Theory]
    [InlineData("unpaid", "active", "price_starter", "cus_first", "cus_first", "1", "complete", "subscription")]
    [InlineData("paid", "canceled", "price_starter", "cus_first", "cus_first", "1", "complete", "subscription")]
    [InlineData("paid", "past_due", "price_starter", "cus_first", "cus_first", "1", "complete", "subscription")]
    [InlineData("paid", "active", "price_other", "cus_first", "cus_first", "1", "complete", "subscription")]
    [InlineData("paid", "active", "price_starter", "cus_second", "cus_second", "1", "complete", "subscription")]
    [InlineData("paid", "active", "price_starter", "cus_first", "cus_second", "1", "complete", "subscription")]
    [InlineData("paid", "active", "price_starter", "cus_first", "cus_first", "2", "complete", "subscription")]
    [InlineData("paid", "active", "price_starter", "cus_first", "cus_first", "1", "open", "subscription")]
    [InlineData("paid", "active", "price_starter", "cus_first", "cus_first", "1", "complete", "payment")]
    public async Task BothCheckoutPathsRequireACurrentPaidClinicSubscription(string payment, string subscriptionStatus,
        string price, string checkoutCustomer, string subscriptionCustomer, string clinic, string checkoutStatus, string mode)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic(null);
        var subscription = StripeBillingFixture.Subscription("sub_checkout", subscriptionStatus, price, subscriptionCustomer);
        billing.StripeResource(Path, Checkout(subscription, payment, checkoutCustomer, clinic, checkoutStatus, mode));
        using var callback = await Complete(billing);
        callback.EnsureSuccessStatusCode();
        using var admin = await billing.App.SignIn(1);
        using var browserReturn = await admin.PostAsJsonAsync("/api/settings/subscription/verify-session", new { sessionId = SessionId });
        browserReturn.EnsureSuccessStatusCode();
        await AssertPlan(billing, "Free", 0, null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ALinkedReceiptRefreshesPaidPricingAndRespectsALocalDowngrade(bool switchedToFree)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        using var admin = await billing.App.SignIn(1);
        if (switchedToFree)
        {
            using var free = await admin.PostAsJsonAsync("/api/settings/subscription/checkout", new { targetPlan = "Free" });
            free.EnsureSuccessStatusCode();
        }
        billing.StripeResource(Path, Checkout(StripeBillingFixture.Subscription("sub_current", price: "price_starter")));
        using var verified = await admin.PostAsJsonAsync("/api/settings/subscription/verify-session", new { sessionId = SessionId });
        verified.EnsureSuccessStatusCode();
        await AssertPlan(billing, switchedToFree ? "Free" : "Starter", 1, "sub_current");
    }

    [Theory]
    [InlineData(900, null, false)]
    [InlineData(1000, null, false)]
    [InlineData(1100, null, true)]
    [InlineData(1000, "sub_current", true)]
    public async Task ReplacementCheckoutsKeepTheNewerSubscription(long created, string? previous, bool replaces)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", created: 1000));
        billing.StripeResource(Path, Checkout(StripeBillingFixture.Subscription("sub_checkout", created: created), previous: previous));

        using var admin = await billing.App.SignIn(1);
        using var browserReturn = await admin.PostAsJsonAsync("/api/settings/subscription/verify-session", new { sessionId = SessionId });
        browserReturn.EnsureSuccessStatusCode();
        await AssertPlan(billing, replaces ? "Starter" : "Professional", replaces ? 1 : 0, replaces ? "sub_checkout" : "sub_current");
    }
}
