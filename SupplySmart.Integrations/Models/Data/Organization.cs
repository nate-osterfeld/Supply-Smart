using System.ComponentModel.DataAnnotations;

namespace SupplySmart.Integrations.Models.Data;

public class Organization : IEntityBase
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdatedDateUtc { get; set; } = DateTime.UtcNow;

    public List<User> Users { get; set; } = new List<User>();
    public List<Location> Locations { get; set; } = new List<Location>();
}
