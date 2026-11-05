using System.Security.Claims;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Endpoints.Endpoints;

public static class UserEndpoints
{
    public static Task<IResult> GetUsers(ClaimsPrincipal principal, IUserService service, ILoggerFactory logger) =>
        Execute(principal, logger, async (orgId, _) => Results.Ok(await service.GetOrganizationUsersAsync(orgId)));

    public static Task<IResult> InviteUser(InviteUserDto dto, ClaimsPrincipal principal, IUserService service, ILoggerFactory logger) =>
        Execute(principal, logger, async (orgId, adminId) => Results.Ok(await service.InviteUserAsync(orgId, dto, adminId)));

    public static Task<IResult> UpdateRole(int userId, UpdateUserRoleDto dto, ClaimsPrincipal principal, IUserService service, ILoggerFactory logger) =>
        Execute(principal, logger, async (orgId, adminId) =>
        {
            var user = await service.UpdateRoleAsync(orgId, userId, dto.Role, adminId);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

    public static Task<IResult> DeleteUser(int userId, ClaimsPrincipal principal, IUserService service, ILoggerFactory logger) =>
        Execute(principal, logger, async (orgId, adminId) =>
            await service.DeleteUserAsync(orgId, userId, adminId) ? Results.Ok() : Results.NotFound());

    private static async Task<IResult> Execute(ClaimsPrincipal principal, ILoggerFactory logger, Func<int, int, Task<IResult>> action)
    {
        if (!int.TryParse(principal.FindFirstValue("OrganizationId"), out var orgId) ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId))
            return Results.Unauthorized();
        try { return await action(orgId, adminId); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
        catch (SmtpException ex)
        {
            logger.CreateLogger("UserPermissions").LogError(ex, "Invitation email delivery failed.");
            return Results.Json(new { message = "The invitation email could not be sent. No account was added. Try again later." }, statusCode: 503);
        }
        catch (DbUpdateException ex)
        {
            logger.CreateLogger("UserPermissions").LogError(ex, "User update conflicted with another change.");
            return Results.Conflict(new { message = "This change could not be saved. Refresh the user list and try again." });
        }
        catch (Exception ex)
        {
            logger.CreateLogger("UserPermissions").LogError(ex, "User permissions request failed.");
            return Results.Json(new { message = "The request could not be completed. Refresh and try again." }, statusCode: 500);
        }
    }
}
