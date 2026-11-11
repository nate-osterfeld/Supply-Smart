using System;

namespace SupplySmart.Integrations.Models.Data;

public class NotificationTemplate : IEntityBase
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    /// <summary>
    /// e.g., "Item Reaches Low-Stock Limit", "Item Falls Out of Stock", "Weekly Inventory Digest"
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;

    public string TemplateMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of external/additional email recipients
    /// </summary>
    public string AdditionalRecipients { get; set; } = string.Empty;

    public DateTime CreatedDateUtc { get; set; }
    public DateTime LastUpdatedDateUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string LastUpdatedBy { get; set; } = string.Empty;
}