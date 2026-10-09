using System.ComponentModel.DataAnnotations;

namespace SupplySmart.Integrations.Models.Data;

public class Location : IEntityBase
{
    [Key]
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    [Required]
    public string Name { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;
    public List<Room> Rooms { get; set; } = new List<Room>();
}