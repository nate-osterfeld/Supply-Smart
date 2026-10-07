using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;
using System.Collections.Generic;

namespace SupplySmart.Endpoints.Configuration
{
    public static partial class EndpointMapper
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var authEndpoints = app.MapGroup("/api/auth").WithTags("Authentication");

            authEndpoints.MapPost("/register", AuthEndpoints.Register)
                .WithName("RegisterOrganization")
                .WithDescription("Provisions a new tenant organization, root admin user, and initial location.")
                .Produces<AuthResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            authEndpoints.MapPost("/login", AuthEndpoints.Login)
                .WithName("LoginUser")
                .WithDescription("Verifies user credentials and returns a signed JWT token.")
                .Produces<AuthResponseDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            authEndpoints.MapGet("/locations", AuthEndpoints.GetLocations)
                .WithName("GetTenantLocations")
                .WithDescription("Retrieves all physical stores/branches tied to the authenticated user's organization.")
                .RequireAuthorization()
                .Produces<List<LocationDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
    }
}