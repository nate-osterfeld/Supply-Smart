using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;

namespace SupplySmart.Host.Authentication;

public sealed class CurrentUserJwtBearerEvents(SupplySmartDbContext db) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        // A valid signature does not prove the account still has these permissions.
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !int.TryParse(context.Principal?.FindFirstValue("OrganizationId"), out var organizationId) ||
            !Guid.TryParse(context.Principal?.FindFirstValue("SecurityStamp"), out var securityStamp) ||
            securityStamp == Guid.Empty)
        {
            context.Fail("Invalid account claims.");
            return;
        }

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.OrganizationId, u.Role, u.SecurityStamp })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is null || user.OrganizationId != organizationId || user.SecurityStamp != securityStamp ||
            !Enum.IsDefined(user.Role) ||
            user.Role.ToString() != context.Principal?.FindFirstValue(ClaimTypes.Role))
        {
            context.Fail("Account access has changed. Sign in again.");
        }
    }
}
