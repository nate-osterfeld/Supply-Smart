using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration;

public static partial class EndpointMapper
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var notificationEndpoints = app.MapGroup("/api/settings/notifications")
            .WithTags("Notification Templates")
            .RequireAuthorization();

        notificationEndpoints.MapGet("/", NotificationEndpoints.GetTemplates)
            .WithName("GetNotificationTemplates")
            .WithDescription("Fetches all custom markdown notification templates and recipient lists for the active tenant organization.")
            .Produces<List<NotificationTemplateDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        notificationEndpoints.MapPut("/", NotificationEndpoints.UpdateTemplate)
            .WithName("UpdateNotificationTemplate")
            .WithDescription("Saves and updates a specific inventory event notification template and additional recipient list.")
            .Produces<NotificationTemplateDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        notificationEndpoints.MapPost("/test", NotificationEndpoints.SendTestAlert)
            .WithName("SendTestNotificationAlert")
            .WithDescription("Dispatches a live test alert email populated with sample data to verify email delivery and recipient routing.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}