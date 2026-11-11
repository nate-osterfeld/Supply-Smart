using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Endpoints;

public abstract class NotificationEndpoints
{
    public static async Task<Results<Ok<List<NotificationTemplateDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetTemplates(
        ClaimsPrincipal claimsPrincipal,
        INotificationService notificationService,
        ILogger<NotificationEndpoints> logger)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                return TypedResults.Unauthorized();

            var templates = await notificationService.GetTemplatesAsync(orgId);
            return TypedResults.Ok(templates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching notification templates.");
            return TypedResults.Problem("Failed to load notification templates.");
        }
    }

    public static async Task<Results<Ok<NotificationTemplateDto>, UnauthorizedHttpResult, ForbidHttpResult, ProblemHttpResult>> UpdateTemplate(
        ClaimsPrincipal claimsPrincipal,
        INotificationService notificationService,
        ILogger<NotificationEndpoints> logger,
        [FromBody] UpdateNotificationTemplateRequestDto dto)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                return TypedResults.Unauthorized();

            var roleClaim = claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase))
                return TypedResults.Forbid();

            var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "Workspace Admin";
            var updated = await notificationService.UpdateTemplateAsync(orgId, dto, userEmail);
            return TypedResults.Ok(updated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating notification template.");
            return TypedResults.Problem("Failed to save notification template.");
        }
    }

    public static async Task<Results<Ok<object>, UnauthorizedHttpResult, ProblemHttpResult>> SendTestAlert(
        ClaimsPrincipal claimsPrincipal,
        INotificationService notificationService,
        ILogger<NotificationEndpoints> logger,
        [FromBody] SendTestNotificationRequestDto dto)
    {
        try
        {
            var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
            if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                return TypedResults.Unauthorized();

            var count = await notificationService.SendTestAlertAsync(orgId, dto);
            return TypedResults.Ok((object)new { recipientCount = count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending test alert email.");
            return TypedResults.Problem("Failed to send test alert email. Check your SMTP/SendGrid configuration.");
        }
    }
}