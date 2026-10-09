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
    }
}