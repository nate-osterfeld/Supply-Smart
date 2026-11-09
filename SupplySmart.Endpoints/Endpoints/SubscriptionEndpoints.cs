using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Endpoints;

public abstract class SubscriptionEndpoints
{
    public static async Task<Results<Ok<SubscriptionDetailsDto>, UnauthorizedHttpResult, ProblemHttpResult>> GetSubscription(
        ClaimsPrincipal claimsPrincipal,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionEndpoints> logger)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
            {
                return TypedResults.Unauthorized();
            }

            var details = await subscriptionService.GetSubscriptionDetailsAsync(orgId);
            return TypedResults.Ok(details);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching subscription details.");
            return TypedResults.Problem("An unexpected error occurred while loading subscription details.");
        }
    }

    public static async Task<Results<Ok<SubscriptionCheckoutResponseDto>, BadRequest<string>, UnauthorizedHttpResult, ForbidHttpResult, ProblemHttpResult>> CheckoutSubscription(
        ClaimsPrincipal claimsPrincipal,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionEndpoints> logger,
        [FromBody] UpdateSubscriptionRequestDto dto)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
            {
                return TypedResults.Unauthorized();
            }

            var roleClaim = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.Forbid();
            }

            var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "admin@supplysmart.io";

            var response = await subscriptionService.UpdateSubscriptionTierAsync(orgId, dto.TargetPlan, userEmail);
            return TypedResults.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating subscription tier.");
            return TypedResults.Problem("An unexpected error occurred while updating your subscription.");
        }
    }

    public static async Task<Results<Ok<SubscriptionPortalResponseDto>, BadRequest<string>, UnauthorizedHttpResult, ForbidHttpResult, ProblemHttpResult>> CreatePortal(
        ClaimsPrincipal claimsPrincipal,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionEndpoints> logger)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
            {
                return TypedResults.Unauthorized();
            }

            var roleClaim = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return TypedResults.Forbid();
            }

            var portalResponse = await subscriptionService.CreateCustomerPortalSessionAsync(orgId);
            return TypedResults.Ok(portalResponse);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Stripe Customer Portal session.");
            return TypedResults.Problem("An unexpected error occurred while opening the billing portal.");
        }
    }

    public static async Task<Results<Ok<SubscriptionDetailsDto>, UnauthorizedHttpResult, ProblemHttpResult>> VerifySession(
        ClaimsPrincipal claimsPrincipal,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionEndpoints> logger,
        [FromBody] VerifySessionRequestDto dto)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
            {
                return TypedResults.Unauthorized();
            }

            var details = await subscriptionService.VerifyAndSyncSessionAsync(orgId, dto.SessionId);
            return TypedResults.Ok(details);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying Stripe session.");
            return TypedResults.Problem("An unexpected error occurred while verifying the payment session.");
        }
    }

    public static async Task<IResult> HandleWebhook(
        HttpRequest request,
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionEndpoints> logger)
    {
        try
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            var signatureHeader = request.Headers["Stripe-Signature"].ToString();

            await subscriptionService.HandleStripeWebhookAsync(json, signatureHeader);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Stripe webhook.");
            return Results.BadRequest($"Webhook error: {ex.Message}");
        }
    }
}