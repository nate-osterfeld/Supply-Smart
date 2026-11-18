using System.Net;
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
}

internal sealed class StripeBillingFixture : IDisposable
{
    private readonly string signingSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public SettingsApp App { get; }
    private readonly HttpClient client;

    public StripeBillingFixture()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StripeSettings:WebhookSecret"] = signingSecret,
            ["StripeSettings:StarterPriceId"] = "price_starter",
            ["StripeSettings:ProfessionalPriceId"] = "price_pro",
            ["StripeSettings:EnterprisePriceId"] = "price_enterprise"
        }).Build();
        App = new SettingsApp(services => services.AddScoped<ISubscriptionService>(sp =>
            new BillingService(sp.GetRequiredService<SupplySmartDbContext>(), config)));
        client = App.NewClient();
    }

    public Task<int> LinkClinic() => App.ReadDb(async db =>
    {
        var org = await db.Organizations.SingleAsync(o => o.Id == 1);
        org.StripeCustomerId = "cus_first";
        org.StripeSubscriptionId = "sub_current";
        org.SubscriptionPlan = "Professional";
        org.MaxRoomsQuota = 15;
        org.MaxItemsQuota = 1500;
        (await db.Organizations.SingleAsync(o => o.Id == 2)).StripeCustomerId = "cus_second";
        return await db.SaveChangesAsync();
    });

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
    }
}
