using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services;

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplateDto>> GetAsync(int organizationId);
    Task<EmailTemplateDto> SaveAsync(int organizationId, int adminId, string trigger, EmailTemplateDraftDto draft);
    Task<string> SendTestAsync(int organizationId, int adminId, string trigger, EmailTemplateDraftDto draft);
}

public sealed class TemplateConflictException() : Exception("This template was changed by someone else. Reload it before saving.");

public class EmailTemplateService(SupplySmartDbContext db, IEmailService emailService) : IEmailTemplateService
{
    private record Definition(string Trigger, string Label, string Subject, string Body, string[] Variables);
    private static readonly string[] ItemVariables = ["organization_name", "item_name", "supply_room", "current_quantity", "minimum_threshold"];
    private static readonly Definition[] Definitions =
    [
        new("low-stock", "Item reaches low-stock limit", "Low stock: {{item_name}}",
            "{{item_name}} is running low at {{supply_room}}.\n\nCurrent stock: {{current_quantity}}\nMinimum stock: {{minimum_threshold}}\n\nPlease check whether this item needs to be reordered.", ItemVariables),
        new("out-of-stock", "Item runs out of stock", "Out of stock: {{item_name}}",
            "{{item_name}} is out of stock at {{supply_room}}.\n\nPlease arrange a restock when you can.", ItemVariables),
        new("weekly-digest", "Weekly inventory summary", "{{organization_name}} inventory summary",
            "Here is the inventory summary for the week ending {{week_ending}}.\n\n{{inventory_summary}}",
            ["organization_name", "week_ending", "inventory_summary"])
    ];
    private static readonly Regex Variable = new(@"\{\{\s*([a-z_]+)\s*\}\}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public async Task<IReadOnlyList<EmailTemplateDto>> GetAsync(int organizationId)
    {
        var saved = await db.EmailTemplates.AsNoTracking().Where(t => t.OrganizationId == organizationId).ToListAsync();
        return Definitions.Select(d => ToDto(d, saved.SingleOrDefault(t => t.Trigger == d.Trigger))).ToArray();
    }

    public async Task<EmailTemplateDto> SaveAsync(int organizationId, int adminId, string trigger, EmailTemplateDraftDto draft)
    {
        var definition = Find(trigger);
        var (subject, body) = Validate(definition, draft);
        var actor = await RequireAdmin(organizationId, adminId);
        var template = await db.EmailTemplates.SingleOrDefaultAsync(t => t.OrganizationId == organizationId && t.Trigger == trigger);
        if ((template?.Revision ?? 0) != draft.Revision) throw new TemplateConflictException();
        if (template is null)
        {
            template = new EmailTemplate { OrganizationId = organizationId, Trigger = trigger };
            db.EmailTemplates.Add(template);
        }
        template.Subject = subject;
        template.Body = body;
        template.Revision++;
        template.UpdatedAtUtc = DateTime.UtcNow;
        template.UpdatedBy = actor.Email;
        db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = organizationId, UserEmail = actor.Email, UserName = actor.FullName,
            ActionType = "Email Template Updated", Action = "Email Template Updated",
            ItemDetails = $"Updated the {definition.Label.ToLowerInvariant()} email template.",
            LocationName = "System Administration"
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { throw new TemplateConflictException(); }
        catch (DbUpdateException) when (template.Revision == 1) { throw new TemplateConflictException(); }
        return ToDto(definition, template);
    }

    public async Task<string> SendTestAsync(int organizationId, int adminId, string trigger, EmailTemplateDraftDto draft)
    {
        var definition = Find(trigger);
        var (subject, body) = Validate(definition, draft);
        var actor = await RequireAdmin(organizationId, adminId);
        var organization = await db.Organizations.AsNoTracking().SingleAsync(o => o.Id == organizationId);
        // Deliberately fictional inventory: a test never changes stock or sends to other users.
        var values = new Dictionary<string, string>
        {
            ["organization_name"] = organization.Name,
            ["item_name"] = "Sample gloves",
            ["supply_room"] = "Sample supply room",
            ["current_quantity"] = trigger == "out-of-stock" ? "0" : "4",
            ["minimum_threshold"] = "10",
            ["week_ending"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["inventory_summary"] = "Sample gloves: 4 remaining (minimum 10).\nSample masks: 0 remaining (minimum 20)."
        };
        await emailService.SendAlertEmailAsync(actor.Email, "[TEST] " + Render(subject, values),
            "This is a SupplySmart test email using sample inventory.\n\n" + Render(body, values));
        return actor.Email;
    }

    private async Task<User> RequireAdmin(int organizationId, int adminId) =>
        await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == adminId && u.OrganizationId == organizationId && u.Role == UserRole.Admin)
            ?? throw new UnauthorizedAccessException();

    private static Definition Find(string trigger) => Definitions.SingleOrDefault(d => d.Trigger == trigger)
        ?? throw new ArgumentException("Choose a supported email trigger.");

    private static (string Subject, string Body) Validate(Definition definition, EmailTemplateDraftDto draft)
    {
        var subject = draft.Subject?.Trim() ?? "";
        var body = draft.Body?.Trim() ?? "";
        if (subject.Length is < 1 or > 200 || subject.Any(char.IsControl))
            throw new ArgumentException("Enter a subject between 1 and 200 characters on one line.");
        if (body.Length is < 1 or > 8000 || body.Any(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t'))
            throw new ArgumentException("Enter a message between 1 and 8,000 characters.");
        foreach (var text in new[] { subject, body })
        {
            foreach (Match match in Variable.Matches(text))
                if (!definition.Variables.Contains(match.Groups[1].Value))
                    throw new ArgumentException($"The variable {{{{{match.Groups[1].Value}}}}} is not supported for this trigger.");
            var remaining = Variable.Replace(text, "");
            if (remaining.Contains("{{") || remaining.Contains("}}"))
                throw new ArgumentException("Check the template variables. Use two opening and two closing braces, such as {{organization_name}}.");
        }
        return (subject, body);
    }

    // Replacement is one pass. Values cannot introduce more template variables.
    public static string Render(string template, IReadOnlyDictionary<string, string> values) =>
        Variable.Replace(template, match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value : throw new ArgumentException($"Missing value for {match.Groups[1].Value}."));

    private static EmailTemplateDto ToDto(Definition definition, EmailTemplate? template) => new()
    {
        Trigger = definition.Trigger, Label = definition.Label, SupportedVariables = definition.Variables,
        Subject = template?.Subject ?? definition.Subject, Body = template?.Body ?? definition.Body,
        Revision = template?.Revision ?? 0, UpdatedAtUtc = template?.UpdatedAtUtc, UpdatedBy = template?.UpdatedBy
    };
}
