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
            .WithDescription("Upgrades or changes the tenant's SaaS subscription tier and updates spatial quotas.")
            .Produces<SubscriptionCheckoutResponseDto>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}