using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class LocationEndpoints
    {
        public static async Task<Results<Ok<IEnumerable<LocationDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetLocations(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var locations = await locationService.GetLocationsAsync(orgId);
                return TypedResults.Ok(locations);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching locations hierarchy.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<IEnumerable<InventoryItemDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetContainerItems(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            [Microsoft.AspNetCore.Mvc.FromQuery] int? containerId)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var items = await locationService.GetContainerItemsAsync(orgId, containerId);
                return TypedResults.Ok(items);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching container items.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<RoomDto>, UnauthorizedHttpResult, BadRequest<string>, ProblemHttpResult>> CreateRoom(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int locationId,
            [Microsoft.AspNetCore.Mvc.FromBody] CreateRoomDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var room = await locationService.CreateRoomAsync(orgId, locationId, dto.Name);
                return TypedResults.Ok(room);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating room.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> UpdateRoom(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int roomId,
            [Microsoft.AspNetCore.Mvc.FromBody] CreateRoomDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var success = await locationService.UpdateRoomAsync(orgId, roomId, dto.Name, userEmail);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating room.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> DeleteRoom(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int roomId)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var success = await locationService.DeleteRoomAsync(orgId, roomId);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting room.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<ContainerDto>, UnauthorizedHttpResult, ProblemHttpResult>> CreateContainer(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int roomId,
            [Microsoft.AspNetCore.Mvc.FromBody] CreateContainerDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var container = await locationService.CreateContainerAsync(orgId, roomId, dto.Name, dto.Kind, dto.ParentContainerId);
                return TypedResults.Ok(container);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating container.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> UpdateContainer(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int containerId,
            [Microsoft.AspNetCore.Mvc.FromBody] CreateContainerDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var success = await locationService.UpdateContainerAsync(orgId, containerId, dto.Name, dto.Kind, userEmail);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating container.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> DeleteContainer(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int containerId)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var success = await locationService.DeleteContainerAsync(orgId, containerId);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting container.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<InventoryItemDto>, UnauthorizedHttpResult, ProblemHttpResult>> AddItem(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int containerId,
            [Microsoft.AspNetCore.Mvc.FromBody] CreateItemDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var item = await locationService.AddItemAsync(orgId, containerId, dto, userEmail);
                return TypedResults.Ok(item);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding item.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> MoveItem(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int itemId,
            [Microsoft.AspNetCore.Mvc.FromBody] MoveItemDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var success = await locationService.MoveItemAsync(orgId, itemId, dto.TargetContainerId, dto.Quantity, dto.MinimumThreshold, userEmail);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error relocating item.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> UpdateItem(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int itemId,
            [Microsoft.AspNetCore.Mvc.FromBody] UpdateItemDto dto)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var success = await locationService.UpdateItemAsync(orgId, itemId, dto, userEmail);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating item.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> DeleteItem(
            ClaimsPrincipal claimsPrincipal,
            ILocationService locationService,
            ILogger<LocationEndpoints> logger,
            int itemId)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();
                var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "system@supplysmart.com";

                var success = await locationService.DeleteItemAsync(orgId, itemId, userEmail);
                return success ? TypedResults.Ok() : TypedResults.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting item.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }
    }
}