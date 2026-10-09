using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.Data;

public class Container
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Self-referencing foreign key for infinite nesting (e.g., Cabinet -> Shelf -> Bin)
    public int? ParentContainerId { get; set; }
    public Container? ParentContainer { get; set; }
    public List<Container> SubContainers { get; set; } = new List<Container>();

    public string Name { get; set; } = string.Empty; // e.g., "Cabinet A", "Shelf 3", "Bin #4"
    public string Kind { get; set; } = "Cabinet";   // "Cabinet", "Shelf", "Bin", "Drawer"
    public string Detail { get; set; } = string.Empty;

    public List<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
