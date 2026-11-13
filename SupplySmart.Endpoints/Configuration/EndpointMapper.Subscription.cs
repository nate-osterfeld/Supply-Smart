using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration;

public static partial class EndpointMapper
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var subscriptionEndpoints = app.MapGroup("/api/settings/subscription")
            .WithTags("SaaS Subscription & Billing")
            .RequireAuthorization();

        subscriptionEndpoints.MapGet("/", SubscriptionEndpoints.GetSubscription)
            .WithName("GetSubscriptionDetails")
            .WithDescription("Fetches the current billing tier, status, renewal date, and live supply room usage metrics.")
            .Produces<SubscriptionDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        subscriptionEndpoints.MapPost("/checkout", SubscriptionEndpoints.CheckoutSubscription)
            .WithName("CheckoutSubscription")
            .WithDescription("Upgrades or changes the tenant's SaaS subscription tier via Stripe Checkout.")
            .Produces<SubscriptionCheckoutResponseDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        subscriptionEndpoints.MapPost("/portal", SubscriptionEndpoints.CreatePortal)
            .WithName("CreateCustomerPortal")
            .WithDescription("Creates a Stripe Customer Portal session for self-service invoice and payment management.")
            .Produces<SubscriptionPortalResponseDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        subscriptionEndpoints.MapPost("/verify-session", SubscriptionEndpoints.VerifySession)
            .WithName("VerifySubscriptionSession")
            .WithDescription("Verifies a completed Stripe Checkout session and immediately syncs the organization quota.")
            .Produces<SubscriptionDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Public webhook endpoint (Stripe calls this directly, no tenant authorization cookie needed)
        app.MapPost("/api/stripe/webhook", SubscriptionEndpoints.HandleWebhook)
            .WithTags("SaaS Subscription & Billing")
            .WithName("StripeWebhook")
            .WithDescription("Receives asynchronous webhook events from Stripe.")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }
}