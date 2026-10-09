using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SupplySmart.Integrations.Models.Data;

public class Room
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // e.g., "Main Supply Closet", "Sterilization Room"
    public string Kind { get; set; } = "Room";       // "Room", "Suite", "Closet"
    public string Detail { get; set; } = string.Empty;

    public List<Container> Containers { get; set; } = new List<Container>();
}
