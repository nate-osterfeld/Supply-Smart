namespace SupplySmart.Integrations.Models.Data;

public class InventoryTransaction
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    /*
     * Nullable so historical transactions remain
     * even if the inventory item is later deleted.
     */
    public int? InventoryItemId { get; set; }

    // checkout, restock, adjustment
    public string TransactionType { get; set; } = string.Empty;

    /*
     * Signed quantity change:
     *
     * Checkout 5 units -> -5
     * Restock 20 units -> +20
     */
    public int QuantityChange { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string UserEmail { get; set; } = string.Empty;

    /*
     * Historical snapshots.
     *
     * These remain even if the inventory item
     * is renamed or deleted later.
     */
    public string ItemName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Organization Organization { get; set; } = null!;

    public InventoryItem? InventoryItem { get; set; }
}