using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class StripePlanChangeTests
{
    private const string PortalUrl = "https://billing.example.test/review";
    private const string CheckoutRoute = "/api/settings/subscription/checkout";

    [Fact]
    public async Task SwitchingALinkedClinicToFreeWaitsForStripeCancellation()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current"));
        billing.StripeResource("billing_portal/sessions", new { id = "bps_fixture", url = PortalUrl });
        using var admin = await billing.App.SignIn(1);
        using var response = await admin.PostAsJsonAsync(CheckoutRoute, new { targetPlan = "Free" });
        response.EnsureSuccessStatusCode();
        var change = await response.Content.ReadFromJsonAsync<SubscriptionCheckoutResponseDto>();
        Assert.False(change!.UpdatedImmediately);
        Assert.Equal(PortalUrl, change.CheckoutUrl);
        var post = Assert.Single(billing.Posts);
        Assert.Equal("/v1/billing_portal/sessions", post.Path);
        Assert.Equal("cus_first", post.Form["customer"].ToString());
        Assert.Equal("subscription_cancel", post.Form["flow_data[type]"].ToString());
        Assert.Equal("sub_current", post.Form["flow_data[subscription_cancel][subscription]"].ToString());
        Assert.Equal("Professional", await Plan(billing));
        Assert.Equal(0, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));

        using var canceled = await billing.Deliver("customer.subscription.deleted",
            StripeBillingFixture.Subscription("sub_current", status: "canceled"));
        canceled.EnsureSuccessStatusCode();
        Assert.Equal("Free", await Plan(billing));
        Assert.Equal(1, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Fact]
    public async Task ALinkedSubscriptionMustMatchTheClinicsCustomerBeforeCancellation()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", customer: "cus_second"));
        using var admin = await billing.App.SignIn(1);
        using var response = await admin.PostAsJsonAsync(CheckoutRoute, new { targetPlan = "Free" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(billing.Posts);
        Assert.Equal("Professional", await Plan(billing));
    }

    [Theory]
    [InlineData("Enterprise", "subscription_update_confirm")]
    [InlineData("Professional", "subscription_update")]
    public async Task PaidChangesUseTheExistingSubscriptionAndKeepEntitlementsUntilConfirmed(string target, string flow)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", price: "price_pro"));
        billing.StripeResource("billing_portal/sessions", new { id = "bps_fixture", url = PortalUrl });
        using var admin = await billing.App.SignIn(1);
        using var response = await admin.PostAsJsonAsync(CheckoutRoute, new { targetPlan = target });
        response.EnsureSuccessStatusCode();
        var change = await response.Content.ReadFromJsonAsync<SubscriptionCheckoutResponseDto>();
        Assert.False(change!.UpdatedImmediately);
        Assert.Equal(PortalUrl, change.CheckoutUrl);
        var post = Assert.Single(billing.Posts);
        Assert.Equal("/v1/billing_portal/sessions", post.Path);
        Assert.Equal("cus_first", post.Form["customer"].ToString());
        Assert.Equal(flow, post.Form["flow_data[type]"].ToString());
        Assert.Equal("sub_current", post.Form[$"flow_data[{flow}][subscription]"].ToString());
        if (flow == "subscription_update_confirm")
        {
            Assert.Equal("si_fixture", post.Form["flow_data[subscription_update_confirm][items][0][id]"].ToString());
            Assert.Equal("price_enterprise", post.Form["flow_data[subscription_update_confirm][items][0][price]"].ToString());
            Assert.Equal("1", post.Form["flow_data[subscription_update_confirm][items][0][quantity]"].ToString());
        }
        Assert.Equal("Professional", await Plan(billing));
        Assert.Equal(0, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("canceled")]
    [InlineData("incomplete_expired")]
    public async Task FirstAndEndedSubscriptionsCanStartANewCheckout(string? previousStatus)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic(previousStatus is null ? null : "sub_current");
        if (previousStatus is not null)
            billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", status: previousStatus));
        billing.StripeResource("checkout/sessions", new { id = "cs_new", url = "https://checkout.example.test/new" });
        using var admin = await billing.App.SignIn(1);
        using var response = await admin.PostAsJsonAsync(CheckoutRoute, new { targetPlan = "Starter" });
        response.EnsureSuccessStatusCode();
        var post = Assert.Single(billing.Posts);
        Assert.Equal("/v1/checkout/sessions", post.Path);
        Assert.Equal("price_starter", post.Form["line_items[0][price]"].ToString());
        Assert.Equal("cus_first", post.Form["customer"].ToString());
    }

    [Fact]
    public async Task AnUnrecognizedSubscriptionItemCannotBeReplacedWithANewCheckout()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", price: "price_unrelated"));
        using var admin = await billing.App.SignIn(1);
        using var response = await admin.PostAsJsonAsync(CheckoutRoute, new { targetPlan = "Enterprise" });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(billing.Posts);
        Assert.Equal("Professional", await Plan(billing));
    }

    private static Task<string> Plan(StripeBillingFixture billing) =>
        billing.App.ReadDb(db => db.Organizations.Where(o => o.Id == 1).Select(o => o.SubscriptionPlan).SingleAsync());
}
