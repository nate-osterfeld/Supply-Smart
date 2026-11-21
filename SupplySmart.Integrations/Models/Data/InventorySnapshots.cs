using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.Data;

public class InventorySnapshot
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public int InventoryItemId { get; set; }
    public InventoryItem InventoryItem { get; set; } = null!;

    public int ContainerIdAtSnapshot { get; set; }
    public Container ContainerAtSnapshot { get; set; } = null!;

    public DateTime SnapshotDateUtc { get; set; } = DateTime.UtcNow;

    public int QuantityOnHand { get; set; }

    public int MinimumThresholdAtSnapshot { get; set; }

    public decimal UnitCostAtSnapshot { get; set; }

    public DateTime? ExpirationDateAtSnapshot { get; set; }

    public string StatusAtSnapshot { get; set; } = "Safe";
}