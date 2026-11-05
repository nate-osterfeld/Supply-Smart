using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetOrganizationUsersAsync(int organizationId);
    Task<UserDto> InviteUserAsync(int organizationId, InviteUserDto dto, int adminId);
    Task<UserDto?> UpdateRoleAsync(int organizationId, int targetUserId, string role, int adminId);
    Task<bool> DeleteUserAsync(int organizationId, int targetUserId, int adminId);
}

public class UserService(SupplySmartDbContext db, IEmailService emailService) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetOrganizationUsersAsync(int organizationId) =>
        await db.Users.AsNoTracking().Where(u => u.OrganizationId == organizationId)
            .OrderBy(u => u.FullName).Select(u => new UserDto
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString()
            }).ToListAsync();

    public async Task<UserDto> InviteUserAsync(int organizationId, InviteUserDto dto, int adminId)
    {
        var name = dto.FullName?.Trim() ?? "";
        var email = dto.Email?.Trim().ToLowerInvariant() ?? "";
        if (name.Length is < 1 or > 150)
            throw new InvalidOperationException("Enter a name between 1 and 150 characters.");
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("Enter a valid email address.");
        var role = ParseRole(dto.Role);
        // Roll back the account and audit entry if SMTP fails.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await RequireAdmin(organizationId, adminId);
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == email))
            throw new InvalidOperationException("A user with this email address already exists.");
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var user = new User { OrganizationId = organizationId, FullName = name, Email = email, Role = role };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        AddAudit(actor, "User Invited", $"Invited {name} ({email}) as {role}.");
        await db.SaveChangesAsync();
        await emailService.SendInviteEmailAsync(email, name, password);
        await transaction.CommitAsync();
        return ToDto(user);
    }

    public async Task<UserDto?> UpdateRoleAsync(int organizationId, int targetUserId, string role, int adminId)
    {
        var parsedRole = ParseRole(role);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await RequireAdmin(organizationId, adminId);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == organizationId);
        if (user is null) return null;
        if (user.Role == parsedRole) return ToDto(user);
        await CheckRemoval(user, adminId);
        var previousRole = user.Role;
        user.Role = parsedRole;
        // Changing back to the old role must not revive an old token.
        user.SessionVersion++;
        AddAudit(actor, "User Role Changed", $"Changed {user.FullName} ({user.Email}) from {previousRole} to {parsedRole}.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return ToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int organizationId, int targetUserId, int adminId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await RequireAdmin(organizationId, adminId);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == organizationId);
        if (user is null) return false;
        await CheckRemoval(user, adminId);
        db.Users.Remove(user);
        AddAudit(actor, "User Revoked", $"Revoked access for {user.FullName} ({user.Email}).");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    private async Task<User> RequireAdmin(int organizationId, int adminId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == adminId && u.OrganizationId == organizationId && u.Role == UserRole.Admin)
            ?? throw new UnauthorizedAccessException("Administrator access is required.");

    private async Task CheckRemoval(User user, int adminId)
    {
        if (user.Id == adminId)
            throw new InvalidOperationException("Another administrator must change your role or remove your account.");
        if (user.Role == UserRole.Admin &&
            await db.Users.CountAsync(u => u.OrganizationId == user.OrganizationId && u.Role == UserRole.Admin) <= 1)
            throw new InvalidOperationException("The organization must keep at least one administrator.");
    }

    private static UserRole ParseRole(string? role)
    {
        // Enum.TryParse also accepts numbers, including undefined enum values.
        var name = Enum.GetNames<UserRole>().FirstOrDefault(n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));
        return name is null
            ? throw new InvalidOperationException("Choose Admin, Manager, or Staff.")
            : Enum.Parse<UserRole>(name);
    }

    private void AddAudit(User actor, string action, string details) => db.AuditLogs.Add(new AuditLog
    {
        OrganizationId = actor.OrganizationId, UserEmail = actor.Email, UserName = actor.FullName,
        ActionType = action, Action = action, ItemDetails = details, LocationName = "System Administration"
    });

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id, FullName = user.FullName, Email = user.Email, Role = user.Role.ToString()
    };
}
