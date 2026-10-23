namespace SupplySmart.Integrations.Models.DTO;

public class MasterCatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string LotNumber { get; set; } = string.Empty;
    public int MinimumThreshold { get; set; }
    public decimal UnitCost { get; set; }
    public int Type { get; set; }
}