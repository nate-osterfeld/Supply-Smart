using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.DbContexts;
using BillingService = SupplySmart.Domain.Services.SubscriptionService;

namespace SupplySmart.Tests;

public class StripeWebhookTests
{
    [Theory]
    [InlineData("customer.subscription.deleted", "cus_first", "sub_previous", "Professional")]
    [InlineData("customer.subscription.updated", "cus_first", "sub_previous", "Professional")]
    [InlineData("customer.subscription.deleted", "cus_second", "sub_current", "Professional")]
    [InlineData("customer.subscription.deleted", "", "sub_current", "Professional")]
    [InlineData("customer.subscription.deleted", "cus_first", "sub_current", "Free")]
    [InlineData("customer.subscription.updated", "cus_first", "sub_current", "Starter")]
    public async Task OnlyTheLinkedSubscriptionCanChangeClinicEntitlements(
        string eventType, string customer, string subscription, string expectedPlan)
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current"));
        using var response = await billing.Deliver(eventType, new
        {
            id = subscription, @object = "subscription", customer,
            status = eventType.EndsWith(".deleted") ? "canceled" : "active",
            items = new { data = new[] { new { price = new { id = "price_starter" } } } }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await billing.App.ReadDb(async db =>
        {
            var org = await db.Organizations.SingleAsync(o => o.Id == 1);
            Assert.Equal(expectedPlan, org.SubscriptionPlan);
            Assert.Equal(expectedPlan switch { "Free" => 50, "Starter" => 250, _ => 1500 }, org.MaxItemsQuota);
            Assert.Equal("Free", (await db.Organizations.SingleAsync(o => o.Id == 2)).SubscriptionPlan);
            Assert.Equal(expectedPlan == "Professional" ? 0 : 1, await db.AuditLogs.CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task DelayedUpdateUsesTheCurrentProviderPrice()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        billing.StripeResource("subscriptions/sub_current", StripeBillingFixture.Subscription("sub_current", price: "price_starter"));
        using var response = await billing.Deliver("customer.subscription.updated",
            StripeBillingFixture.Subscription("sub_current", price: "price_enterprise"));
        response.EnsureSuccessStatusCode();
        Assert.Equal("Starter", await billing.App.ReadDb(db => db.Organizations.Where(o => o.Id == 1).Select(o => o.SubscriptionPlan).SingleAsync()));
    }

    [Fact]
    public async Task AnUpdateAlreadyInFlightCannotUndoACommittedCancellation()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        var active = StripeBillingFixture.Subscription("sub_current");
        var canceled = StripeBillingFixture.Subscription("sub_current", status: "canceled");
        billing.StripeResource("subscriptions/sub_current", active);
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        billing.BeforeStripeReply = () =>
        {
            reading.TrySetResult();
            return resume.Task.WaitAsync(TimeSpan.FromSeconds(10));
        };
        var pending = billing.Deliver("customer.subscription.updated", active);
        try
        {
            await reading.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using var deletion = await billing.Deliver("customer.subscription.deleted", canceled);
            deletion.EnsureSuccessStatusCode();
        }
        finally
        {
            resume.TrySetResult();
        }
        using var stale = await pending;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, stale.StatusCode);
        Assert.DoesNotContain("billing state", await stale.Content.ReadAsStringAsync());
        billing.StripeResource("subscriptions/sub_current", canceled);
        using var retry = await billing.Deliver("customer.subscription.updated", active);
        retry.EnsureSuccessStatusCode();
        await billing.App.ReadDb(async db =>
        {
            var org = await db.Organizations.SingleAsync(o => o.Id == 1);
            Assert.Equal(("Free", "Canceled", 50), (org.SubscriptionPlan, org.SubscriptionStatus, org.MaxItemsQuota));
            Assert.Equal(1, await db.AuditLogs.CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task AReceiverWithoutASigningSecretLeavesDeliveryPending()
    {
        using var billing = new StripeBillingFixture(signingConfigured: false);
        await billing.LinkClinic();
        using var response = await billing.Deliver("customer.subscription.deleted",
            StripeBillingFixture.Subscription("sub_current", status: "canceled"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("t=1,v1=unrecognized")]
    public async Task UnverifiedNotificationsReceiveAGenericClientError(string? signature)
    {
        using var billing = new StripeBillingFixture();
        using var client = billing.App.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = JsonContent.Create(new { id = "evt_fixture", @object = "event" })
        };
        if (signature is not null) request.Headers.Add("Stripe-Signature", signature);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid Stripe notification.", await response.Content.ReadFromJsonAsync<string>());
        Assert.Equal(0, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Fact]
    public async Task AProviderOutageCanBeRetriedWithoutExposingItsDiagnostics()
    {
        using var billing = new StripeBillingFixture();
        await billing.LinkClinic();
        var subscription = StripeBillingFixture.Subscription("sub_current");
        billing.StripeResource("subscriptions/sub_current", subscription);
        billing.BeforeStripeReply = () => throw new HttpRequestException("Local provider diagnostic.");
        using var outage = await billing.Deliver("customer.subscription.updated", subscription);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, outage.StatusCode);
        Assert.Empty(await outage.Content.ReadAsStringAsync());
        Assert.Equal(0, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
        billing.BeforeStripeReply = null;
        using var retry = await billing.Deliver("customer.subscription.updated", subscription);
        retry.EnsureSuccessStatusCode();
        Assert.Equal(1, await billing.App.ReadDb(db => db.AuditLogs.CountAsync()));
    }
}

internal sealed class StripeBillingFixture : IDisposable
{
    private readonly string signingSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public SettingsApp App { get; }
    private readonly HttpClient client;
    private readonly LocalStripe transport = new();
    private readonly HttpClient stripeHttp;

    public StripeBillingFixture(bool signingConfigured = true)
    {
        stripeHttp = new HttpClient(transport);
        var stripe = new StripeClient("sk_test_fixture", httpClient: new SystemNetHttpClient(stripeHttp, maxNetworkRetries: 0, enableTelemetry: false));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StripeSettings:WebhookSecret"] = signingConfigured ? signingSecret : "",
            ["StripeSettings:StarterPriceId"] = "price_starter",
            ["StripeSettings:ProfessionalPriceId"] = "price_pro",
            ["StripeSettings:EnterprisePriceId"] = "price_enterprise"
        }).Build();
        App = new SettingsApp(services => services.AddScoped<ISubscriptionService>(sp =>
            new BillingService(sp.GetRequiredService<SupplySmartDbContext>(), config, stripe)));
        client = App.NewClient();
    }

    public Task<int> LinkClinic(string? subscriptionId = "sub_current") => App.ReadDb(async db =>
    {
        var org = await db.Organizations.SingleAsync(o => o.Id == 1);
        org.StripeCustomerId = "cus_first";
        org.StripeSubscriptionId = subscriptionId;
        if (subscriptionId is not null)
        {
            org.SubscriptionPlan = "Professional";
            org.MaxRoomsQuota = 15;
            org.MaxItemsQuota = 1500;
        }
        (await db.Organizations.SingleAsync(o => o.Id == 2)).StripeCustomerId = "cus_second";
        return await db.SaveChangesAsync();
    });

    public void StripeResource(string path, object resource) => transport.Resources["/v1/" + path] = resource;
    public Func<Task>? BeforeStripeReply { set => transport.BeforeReply = value; }

    public static object Subscription(string id, string status = "active", string price = "price_starter",
        string customer = "cus_first", long created = 1000) => new
    {
        id, @object = "subscription", customer, status, created,
        items = new { @object = "list", data = new[] { new { quantity = 1, price = new { id = price, @object = "price" } } } }
    };

    private sealed class LocalStripe : HttpMessageHandler
    {
        public Dictionary<string, object> Resources { get; } = [];
        public Func<Task>? BeforeReply { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            var uri = request.RequestUri!;
            if (uri.AbsolutePath.StartsWith("/v1/checkout/sessions/"))
                Assert.Contains("subscription", Uri.UnescapeDataString(uri.Query));
            Assert.True(Resources.TryGetValue(uri.AbsolutePath, out var result), $"Unexpected Stripe read: {uri.AbsolutePath}");
            if (BeforeReply is not null) await BeforeReply();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(result) };
        }
    }

    public async Task<HttpResponseMessage> Deliver(string eventType, object resource)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_fixture", @object = "event", type = eventType, created = timestamp,
            api_version = StripeConfiguration.ApiVersion, data = new { @object = resource }
        });
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={Convert.ToHexStringLower(digest)}");
        return await client.SendAsync(request);
    }

    public void Dispose()
    {
        client.Dispose();
        App.Dispose();
        stripeHttp.Dispose();
    }
}
