using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration
{
    public static partial class EndpointMapper
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var userEndpoints = app.MapGroup("/api/settings/users")
                .WithTags("Tenant User Permissions")
                .RequireAuthorization();

            userEndpoints.MapGet("/", UserEndpoints.GetUsers)
                .WithName("GetTenantUsers")
                .WithDescription("Fetch tenant users (Accessible to authenticated members)")
                .Produces<IEnumerable<UserDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized);

            userEndpoints.MapPost("/invite", UserEndpoints.InviteUser)
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .WithName("InviteTenantUser")
                .WithDescription("Invite user (Restricted strictly to Admins / Super Administrators)")
                .Produces<UserDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized);

            userEndpoints.MapDelete("/{userId}", UserEndpoints.DeleteUser)
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .WithName("RevokeTenantUser")
                .WithDescription("Revoke user access (Restricted strictly to Admins)")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status401Unauthorized);
        }
    }
}