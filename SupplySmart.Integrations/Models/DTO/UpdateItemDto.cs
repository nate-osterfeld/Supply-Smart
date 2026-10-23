namespace SupplySmart.Integrations.Models.DTO;

public class UpdateItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public int Quantity { get; set; }
    public int MinimumThreshold { get; set; } = 5;
    public decimal UnitCost { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Type { get; set; } // 0 = Consumable, 1 = Reusable
}
