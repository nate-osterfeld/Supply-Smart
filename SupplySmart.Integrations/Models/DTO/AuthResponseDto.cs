namespace SupplySmart.Integrations.Models.DTO;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public List<LocationDto> Locations { get; set; } = new();
}
