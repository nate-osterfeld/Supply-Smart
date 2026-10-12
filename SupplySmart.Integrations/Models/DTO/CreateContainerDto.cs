namespace SupplySmart.Integrations.Models.DTO;

public class CreateContainerDto
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Bin"; // Bin, Shelf, Cabinet, etc.
    public int? ParentContainerId { get; set; }
}