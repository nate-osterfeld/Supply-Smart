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

            locationEndpoints.MapPost("/rooms/{locationId}", LocationEndpoints.CreateRoom)
                .WithName("CreateRoom")
                .WithDescription("Creates a new room within a specified physical location.")
                .Produces<RoomDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            locationEndpoints.MapPost("/rooms/{roomId}/containers", LocationEndpoints.CreateContainer)
                .WithName("CreateContainer")
                .WithDescription("Creates a new storage bin or shelf inside a specified room.")
                .Produces<ContainerDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            locationEndpoints.MapPost("/containers/{containerId}/items", LocationEndpoints.AddItem)
                .WithName("AddItemToContainer")
                .WithDescription("Adds a new inventory item to a specific container and logs an audit trail event.")
                .Produces<InventoryItemDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            locationEndpoints.MapPost("/items/{itemId}/move", LocationEndpoints.MoveItem)
                .WithName("MoveItemBetweenContainers")
                .WithDescription("Relocates an inventory item to a different container and records an immutable audit ledger entry.")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
    }
}