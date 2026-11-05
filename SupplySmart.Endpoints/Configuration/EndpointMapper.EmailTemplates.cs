using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration;

public static partial class EndpointMapper
{
    public static void MapEmailTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings/email-templates")
            .WithTags("Email Templates")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapGet("/", (ClaimsPrincipal principal, IEmailTemplateService service, ILoggerFactory logger) =>
            HandleTemplateRequest(principal, logger, async (org, _) => Results.Ok(await service.GetAsync(org))));
        group.MapPut("/{trigger}", (string trigger, EmailTemplateDraftDto draft, ClaimsPrincipal principal, IEmailTemplateService service, ILoggerFactory logger) =>
            HandleTemplateRequest(principal, logger, async (org, admin) => Results.Ok(await service.SaveAsync(org, admin, trigger, draft))));
        group.MapPost("/{trigger}/test", (string trigger, EmailTemplateDraftDto draft, ClaimsPrincipal principal, IEmailTemplateService service, ILoggerFactory logger) =>
            HandleTemplateRequest(principal, logger, async (org, admin) =>
            {
                var recipient = await service.SendTestAsync(org, admin, trigger, draft);
                return Results.Ok(new { recipient, message = "Test email sent." });
            })).RequireRateLimiting("email-template-tests");
    }

    private static async Task<IResult> HandleTemplateRequest(ClaimsPrincipal principal, ILoggerFactory logger, Func<int, int, Task<IResult>> action)
    {
        if (!int.TryParse(principal.FindFirstValue("OrganizationId"), out var orgId) ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
            return Results.Unauthorized();
        try { return await action(orgId, adminId); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (TemplateConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (SmtpException ex)
        {
            logger.CreateLogger("EmailTemplates").LogError(ex, "Template test email failed.");
            return Results.Json(new { message = "The test email could not be sent. Check the email service and try again later." }, statusCode: 503);
        }
        catch (Exception ex)
        {
            logger.CreateLogger("EmailTemplates").LogError(ex, "Email template request failed.");
            return Results.Json(new { message = "The request could not be completed. Try again later." }, statusCode: 500);
        }
    }
}
