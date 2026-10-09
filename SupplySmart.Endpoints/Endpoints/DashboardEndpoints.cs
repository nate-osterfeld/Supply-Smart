using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class DashboardEndpoints
    {
        public static async Task<Results<Ok<DashboardMetricsDto>, UnauthorizedHttpResult, ProblemHttpResult>> GetMetrics(
            ClaimsPrincipal claimsPrincipal,
            IDashboardService dashboardService,
            ILogger<DashboardEndpoints> logger,
            [Microsoft.AspNetCore.Mvc.FromQuery] int? locationId)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var metrics = await dashboardService.GetMetricsAsync(orgId, locationId);
                return TypedResults.Ok(metrics);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching dashboard metrics.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<ProcedureForecastSummaryDto>, UnauthorizedHttpResult, ProblemHttpResult>> GetForecastSummary(
            ClaimsPrincipal claimsPrincipal,
            IDashboardService dashboardService,
            ILogger<DashboardEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var forecast = await dashboardService.GetForecastSummaryAsync(orgId);
                return TypedResults.Ok(forecast);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching forecast summary.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<IEnumerable<AuditLogEntryDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetRecentAuditLogs(
            ClaimsPrincipal claimsPrincipal,
            IDashboardService dashboardService,
            ILogger<DashboardEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var logs = await dashboardService.GetRecentAuditLogsAsync(orgId, limit: 5);
                return TypedResults.Ok(logs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching recent audit logs.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }
    }
}