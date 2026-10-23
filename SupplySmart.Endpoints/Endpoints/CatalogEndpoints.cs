using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class CatalogEndpoints
    {
        public static async Task<Results<Ok<IEnumerable<MasterCatalogItemDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetMasterCatalog(
            ClaimsPrincipal claimsPrincipal, ICatalogService catalogService, ILogger<CatalogEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var catalog = await catalogService.GetMasterCatalogAsync(orgId);
                return TypedResults.Ok(catalog);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching master catalog.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<IEnumerable<AuditLogEntryDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetAuditLogs(
            ClaimsPrincipal claimsPrincipal, ICatalogService catalogService, ILogger<CatalogEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId)) return TypedResults.Unauthorized();

                var logs = await catalogService.GetAuditLogsAsync(orgId);
                return TypedResults.Ok(logs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching audit logs.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }
    }
}