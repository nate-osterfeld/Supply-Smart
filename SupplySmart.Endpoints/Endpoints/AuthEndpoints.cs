using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using SupplySmart.Endpoints.Security;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class AuthEndpoints
    {
        public static async Task<Results<Ok<AuthResponseDto>, BadRequest<object>, ProblemHttpResult>> Register(
            RegisterDto dto,
            HttpContext context,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            try
            {
                var response = await authService.RegisterAsync(dto);
                BrowserSession.CompleteSignIn(context, response);
                return TypedResults.Ok(response);
            }
            catch (ValidationException ex)
            {
                return TypedResults.BadRequest<object>(new { message = ex.Message });
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

        public static async Task<Results<Ok<AuthResponseDto>, BadRequest<object>, UnauthorizedHttpResult, ProblemHttpResult>> Login(
            LoginDto dto,
            HttpContext context,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            try
            {
                var response = await authService.LoginAsync(dto);
                BrowserSession.CompleteSignIn(context, response);
                return TypedResults.Ok(response);
            }
            catch (ValidationException ex)
            {
                return TypedResults.BadRequest<object>(new { message = ex.Message });
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

        public static async Task<Results<NoContent, UnauthorizedHttpResult, ProblemHttpResult>> Logout(
            ClaimsPrincipal claimsPrincipal,
            HttpContext context,
            IAuthService authService,
            ILogger<AuthEndpoints> logger)
        {
            if (!int.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return TypedResults.Unauthorized();
            try
            {
                await authService.LogoutAsync(userId);
                BrowserSession.Clear(context);
                return TypedResults.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ending account sessions.");
                return TypedResults.Problem("Unable to sign out. Please try again.");
            }
        }

        public sealed record BrowserSessionState(AuthResponseDto? User, string? CsrfToken);

        public static async Task<Ok<BrowserSessionState>> GetBrowserSession(HttpContext context, IAuthService authService)
        {
            context.Response.Headers.CacheControl = "no-store";
            var claims = context.User;
            var csrfToken = claims.FindFirstValue("jti");
            if (!context.Request.Cookies.ContainsKey(BrowserSession.CookieName) ||
                claims.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(csrfToken) ||
                !int.TryParse(claims.FindFirstValue("OrganizationId"), out var organizationId))
                return TypedResults.Ok(new BrowserSessionState(null, null));

            var profile = new AuthResponseDto
            {
                Email = claims.FindFirstValue(ClaimTypes.Email)!,
                FullName = claims.FindFirstValue(ClaimTypes.Name)!,
                Role = claims.FindFirstValue(ClaimTypes.Role)!,
                OrganizationId = organizationId,
                Locations = await authService.GetLocationsForUserAsync(organizationId)
            };
            return TypedResults.Ok(new BrowserSessionState(profile, csrfToken));
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
