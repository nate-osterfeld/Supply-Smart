using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration;

public static partial class EndpointMapper
{
    public static IEndpointRouteBuilder MapForecastEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        app.MapGet(
                "/api/forecast",
                ForecastEndpoints.GetForecast
            )
            .WithName("GetInventoryForecast")
            .RequireAuthorization()
            .Produces<ForecastResponseDto>(
                StatusCodes.Status200OK
            )
            .Produces(
                StatusCodes.Status401Unauthorized
            )
            .ProducesProblem(
                StatusCodes.Status500InternalServerError
            );

        return app;
    }
}