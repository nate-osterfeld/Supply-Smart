public class AuditLogEntryDto
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ItemDetails { get; set; } = string.Empty;
    public string LocationContext { get; set; } = string.Empty;
}