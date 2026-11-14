using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using Microsoft.Extensions.Logging;

namespace SupplySmart.Endpoints.Endpoints;

public static class ForecastEndpoints
{
    public static async Task<
        Results<
            Ok<ForecastResponseDto>,
            UnauthorizedHttpResult,
            ProblemHttpResult
        >
    > GetForecast(
        ClaimsPrincipal claimsPrincipal,
        IForecastService forecastService,
        ILogger<ForecastService> logger,
        int days = 7
    )
    {
        try
        {
            var organizationIdClaim =
                claimsPrincipal.FindFirst("OrganizationId")?.Value;

            if (
                string.IsNullOrWhiteSpace(organizationIdClaim) ||
                !int.TryParse(
                    organizationIdClaim,
                    out var organizationId
                )
            )
            {
                return TypedResults.Unauthorized();
            }

            var forecast =
                await forecastService.GetForecastAsync(
                    organizationId,
                    days
                );

            return TypedResults.Ok(forecast);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while generating inventory forecast."
            );

            return TypedResults.Problem(
                "An error occurred while generating the inventory forecast."
            );
        }
    }
}