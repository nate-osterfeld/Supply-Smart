namespace SupplySmart.Integrations.Models.DTO;

public class AuthResponseDto
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int OrganizationId { get; set; }
    public List<LocationDto> Locations { get; set; } = new();
}
