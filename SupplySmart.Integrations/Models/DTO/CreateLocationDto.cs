namespace SupplySmart.Integrations.Models.DTO;

public class CreateLocationDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}