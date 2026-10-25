using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;
using System.Security.Claims;

namespace SupplySmart.Endpoints.Endpoints
{
    public abstract class UserEndpoints
    {
        public static async Task<Results<Ok<IEnumerable<UserDto>>, UnauthorizedHttpResult, ProblemHttpResult>> GetUsers(
            ClaimsPrincipal claimsPrincipal,
            [FromServices] IUserService userService,
            [FromServices] ILogger<UserEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var users = await userService.GetOrganizationUsersAsync(orgId);
                return TypedResults.Ok(users);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching tenant users.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok<UserDto>, BadRequest<object>, UnauthorizedHttpResult, ProblemHttpResult>> InviteUser(
            [FromBody] InviteUserDto dto,
            ClaimsPrincipal claimsPrincipal,
            [FromServices] IUserService userService,
            [FromServices] ILogger<UserEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                var adminEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "admin@supplysmart.io";

                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var newUser = await userService.InviteUserAsync(orgId, dto, adminEmail);
                return TypedResults.Ok(newUser);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "User invitation failed: {Message}", ex.Message);
                return TypedResults.BadRequest<object>(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error inviting user.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }

        public static async Task<Results<Ok, BadRequest<object>, NotFound, UnauthorizedHttpResult, ProblemHttpResult>> DeleteUser(
            int userId,
            ClaimsPrincipal claimsPrincipal,
            [FromServices] IUserService userService,
            [FromServices] ILogger<UserEndpoints> logger)
        {
            try
            {
                var orgIdClaim = claimsPrincipal.FindFirst("OrganizationId")?.Value;
                var adminEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "admin@supplysmart.io";

                if (orgIdClaim == null || !int.TryParse(orgIdClaim, out var orgId))
                {
                    return TypedResults.Unauthorized();
                }

                var success = await userService.DeleteUserAsync(orgId, userId, adminEmail);
                if (!success) return TypedResults.NotFound();

                return TypedResults.Ok();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "User deletion blocked: {Message}", ex.Message);
                return TypedResults.BadRequest<object>(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error revoking user access.");
                return TypedResults.Problem("An unexpected error occurred.");
            }
        }
    }
}