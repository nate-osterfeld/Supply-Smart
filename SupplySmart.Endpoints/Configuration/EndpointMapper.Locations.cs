using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupplySmart.Endpoints.Endpoints;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Configuration
{
    public static partial class EndpointMapper
    {
        public static void MapLocationEndpoints(this WebApplication app)
        {
            var locationEndpoints = app.MapGroup("/api/locations")
                .WithTags("Locations & Spatial Hierarchy")
                .RequireAuthorization();

            locationEndpoints.MapGet("/", LocationEndpoints.GetLocations)
                .WithName("GetLocationsHierarchy")
                .WithDescription("Fetches physical rooms, containers, and bin layouts for the active tenant.")
                .Produces<IEnumerable<LocationDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            locationEndpoints.MapGet("/items", LocationEndpoints.GetContainerItems)
                .WithName("GetContainerItems")
                .WithDescription("Fetches items housed in a specific storage container or bin.")
                .Produces<IEnumerable<InventoryItemDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
    }
}