using System;
using System.Collections.Generic;

namespace SupplySmart.Integrations.Models.DTO;

public class NotificationTemplateDto
{
    public int Id { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string TemplateMarkdown { get; set; } = string.Empty;
    public List<string> AdditionalRecipients { get; set; } = new();
    public string LastUpdatedBy { get; set; } = string.Empty;
    public DateTime LastUpdatedDateUtc { get; set; }
}

public class UpdateNotificationTemplateRequestDto
{
    public string TriggerType { get; set; } = string.Empty;
    public string TemplateMarkdown { get; set; } = string.Empty;
    public List<string> AdditionalRecipients { get; set; } = new();
}

public class SendTestNotificationRequestDto
{
    public string TriggerType { get; set; } = string.Empty;
    public string TemplateMarkdown { get; set; } = string.Empty;
    public List<string> AdditionalRecipients { get; set; } = new();
}