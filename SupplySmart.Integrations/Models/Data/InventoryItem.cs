using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.Data;

public enum ItemType
{
    Consumable = 0, // Single-use, quantity decreases on check-out
    Reusable = 1    // Returnable asset, tracked by custody/status
}

public class InventoryItem
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public int ContainerId { get; set; }
    public Container Container { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ItemType Type { get; set; } = ItemType.Consumable;
    public int Quantity { get; set; }
    public int MinimumThreshold { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Status { get; set; } = "Safe"; // Safe, Expiring Soon, Low Stock
    public bool IsRestockPending { get; set; }
}