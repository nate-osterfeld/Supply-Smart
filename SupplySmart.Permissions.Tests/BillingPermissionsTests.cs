using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class BillingPermissionsTests
{
    [Theory]
    [InlineData(0, HttpStatusCode.Unauthorized)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(3, HttpStatusCode.Forbidden)]
    [InlineData(4, HttpStatusCode.Forbidden)]
    [InlineData(5, HttpStatusCode.OK)]
    public async Task OnlyAdminsCanSynchronizePaidEntitlements(int userId, HttpStatusCode expected)
    {
        var billing = new RecordingBillingService();
        using var app = new SettingsApp(services =>
        {
            services.RemoveAll<ISubscriptionService>();
            services.AddSingleton<ISubscriptionService>(billing);
        });
        using var client = userId == 0 ? app.NewClient() : await app.SignIn(userId);
        var response = await client.PostAsJsonAsync("/api/settings/subscription/verify-session?organizationId=2",
            new { sessionId = "cs_local_test" });

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
            Assert.Equal((userId == 5 ? 2 : 1, "cs_local_test"), Assert.Single(billing.Verifications));
        else
            Assert.Empty(billing.Verifications);
    }

    private sealed class RecordingBillingService : ISubscriptionService
    {
        public List<(int OrganizationId, string SessionId)> Verifications { get; } = [];
        public Task<SubscriptionDetailsDto> VerifyAndSyncSessionAsync(int organizationId, string sessionId)
        {
            Verifications.Add((organizationId, sessionId));
            return Task.FromResult(new SubscriptionDetailsDto());
        }
        public Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(int organizationId) => throw new NotSupportedException();
        public Task<SubscriptionCheckoutResponseDto> UpdateSubscriptionTierAsync(int organizationId, string targetPlan, string userEmail) => throw new NotSupportedException();
        public Task<SubscriptionPortalResponseDto> CreateCustomerPortalSessionAsync(int organizationId) => throw new NotSupportedException();
        public Task HandleStripeWebhookAsync(string jsonPayload, string stripeSignatureHeader) => throw new NotSupportedException();
    }
}
