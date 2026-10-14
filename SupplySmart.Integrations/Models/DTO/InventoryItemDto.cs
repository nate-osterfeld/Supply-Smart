namespace SupplySmart.Integrations.Models.DTO
{
    public class InventoryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public List<string> Statuses { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public int Type { get; set; } // (0 = Consumable, 1 = Reusable)
    }
}