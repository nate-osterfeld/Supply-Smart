using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class AuthEndpoints
    {
        public static async Task<Results<Ok<AuthResponseDto>, BadRequest<object>, ProblemHttpResult>> Register(
            RegisterDto dto,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            try
            {
                var response = await authService.RegisterAsync(dto);
                return TypedResults.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
                return TypedResults.BadRequest<object>(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during registration.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<AuthResponseDto>, UnauthorizedHttpResult, ProblemHttpResult>> Login(
            LoginDto dto,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            try
            {
                var response = await authService.LoginAsync(dto);
                return TypedResults.Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
                return TypedResults.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during login.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<List<LocationDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetLocations(
            ClaimsPrincipal claimsPrincipal,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var locations = await authService.GetLocationsForUserAsync(orgId);
                return TypedResults.Ok(locations);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching locations.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }
    }
}