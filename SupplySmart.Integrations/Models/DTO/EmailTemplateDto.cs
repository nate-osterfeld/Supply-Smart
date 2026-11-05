namespace SupplySmart.Integrations.Models.DTO;

public class EmailTemplateDraftDto
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Revision { get; set; }
}

public class EmailTemplateDto : EmailTemplateDraftDto
{
    public string Trigger { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string[] SupportedVariables { get; set; } = [];
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
