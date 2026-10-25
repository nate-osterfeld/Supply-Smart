using Microsoft.AspNetCore.Identity;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;
using SupplySmart.Integrations.Repositories;

namespace SupplySmart.Domain.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetOrganizationUsersAsync(int organizationId);
        Task<UserDto> InviteUserAsync(int organizationId, InviteUserDto dto, string adminEmail);
        Task<bool> DeleteUserAsync(int organizationId, int targetUserId, string adminEmail);
    }

    public class UserService : IUserService
    {
        private readonly IBaseRepository<User> _userRepo;
        private readonly IBaseRepository<AuditLog> _auditRepo;
        private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public UserService(IBaseRepository<User> userRepo, IBaseRepository<AuditLog> auditRepo)
        {
            _userRepo = userRepo;
            _auditRepo = auditRepo;
        }

        public async Task<IEnumerable<UserDto>> GetOrganizationUsersAsync(int organizationId)
        {
            var users = await _userRepo.GetAllByConditionAsync(u => u.OrganizationId == organizationId);
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString()
            });
        }

        public async Task<UserDto> InviteUserAsync(int organizationId, InviteUserDto dto, string adminEmail)
        {
            var existingUser = await _userRepo.GetByConditionAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email address already exists.");
            }

            if (!Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
            {
                parsedRole = UserRole.Manager;
            }

            // Generate a secure random temporary password
            var tempPassword = GenerateSecureTempPassword();

            var newUser = new User
            {
                OrganizationId = organizationId,
                FullName = dto.FullName,
                Email = dto.Email,
                Role = parsedRole,
                // Optional: Add a flag to your User entity like IsTemporaryPassword = true if tracking forced resets
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, tempPassword);

            await _userRepo.AddAsync(newUser);

            // Simulate sending an invitation email (In production, replace with IEmailService)
            System.Diagnostics.Debug.WriteLine($"[EMAIL SERVICE] Sending invite to {dto.Email}. Temporary Password: {tempPassword}");

            // Log immutable audit entry
            await _auditRepo.AddAsync(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = adminEmail,
                ActionType = "User Invited",
                ItemDetails = $"Invited user '{dto.FullName}' ({dto.Email}) with role '{parsedRole}' and generated temp password.",
                LocationName = "System Administration"
            });

            return new UserDto
            {
                Id = newUser.Id,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Role = newUser.Role.ToString()
            };
        }

        private string GenerateSecureTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> DeleteUserAsync(int organizationId, int targetUserId, string adminEmail)
        {
            var user = await _userRepo.GetByConditionAsync(u => u.Id == targetUserId && u.OrganizationId == organizationId);
            if (user == null) return false;

            // 1. Prevent self-deletion
            if (user.Email.Equals(adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("You cannot delete your own active administrator account.");
            }

            // 2. Prevent deleting the last Admin in the organization
            if (user.Role == UserRole.Admin)
            {
                var allAdmins = await _userRepo.GetAllByConditionAsync(u => u.OrganizationId == organizationId && u.Role == UserRole.Admin);
                if (allAdmins.Count() <= 1)
                {
                    throw new InvalidOperationException("Cannot delete the last remaining administrator for this organization.");
                }
            }

            await _userRepo.DeleteAsync(user.Id);

            await _auditRepo.AddAsync(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = adminEmail,
                ActionType = "User Revoked",
                ItemDetails = $"Revoked access for user '{user.FullName}' ({user.Email})",
                LocationName = "System Administration"
            });

            return true;
        }
    }
}