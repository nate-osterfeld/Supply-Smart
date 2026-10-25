namespace SupplySmart.Integrations.Models.DTO;

public class InviteUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Manager"; // Default role for invites
}