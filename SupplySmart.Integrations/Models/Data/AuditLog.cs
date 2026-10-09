using System;
using System.Collections.Generic;
using System.Text;

namespace SupplySmart.Integrations.Models.Data;

public class AuditLog
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // checkout, restock, adjustment
    public string Action { get; set; } = string.Empty;
    public string ItemDetails { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
}
