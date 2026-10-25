using System.ComponentModel.DataAnnotations;

namespace SupplySmart.Integrations.Models.Data;

public enum UserRole
{
    Admin,
    Manager,
    Staff
}

public class User : IEntityBase
{
    [Key]
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;
}
