namespace SupplySmart.Integrations.Models.Data;

public interface IEntityBase
{
    string CreatedBy { get; set; }
    DateTime CreatedDateUtc { get; set; }
    string LastUpdatedBy { get; set; }
    DateTime LastUpdatedDateUtc { get; set; }
}