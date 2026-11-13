using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services;

public interface INotificationService
{
    Task<List<NotificationTemplateDto>> GetTemplatesAsync(int organizationId);
    Task<NotificationTemplateDto> UpdateTemplateAsync(int organizationId, UpdateNotificationTemplateRequestDto dto, string updatedByEmail);
    Task<int> SendTestAlertAsync(int organizationId, SendTestNotificationRequestDto dto);
    Task CheckAndTriggerItemStockAlertAsync(int organizationId, int previousQuantity, InventoryItem updatedItem);
}

public class NotificationService : INotificationService
{
    public const string TriggerLowStock = "Item Reaches Low-Stock Limit";
    public const string TriggerOutOfStock = "Item Falls Out of Stock";
    public const string TriggerWeeklyDigest = "Weekly Inventory Digest";

    private readonly SupplySmartDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        SupplySmartDbContext context,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<NotificationTemplateDto>> GetTemplatesAsync(int organizationId)
    {
        var templates = await EnsureDefaultTemplatesAsync(organizationId);
        return templates.Select(MapToDto).ToList();
    }

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(
        int organizationId,
        UpdateNotificationTemplateRequestDto dto,
        string updatedByEmail)
    {
        var templates = await EnsureDefaultTemplatesAsync(organizationId);
        var target = templates.FirstOrDefault(t => t.TriggerType == dto.TriggerType);

        if (target == null)
        {
            target = new NotificationTemplate
            {
                OrganizationId = organizationId,
                TriggerType = dto.TriggerType,
                CreatedBy = updatedByEmail
            };
            _context.NotificationTemplates.Add(target);
        }

        target.TemplateMarkdown = dto.TemplateMarkdown;
        target.AdditionalRecipients = string.Join(",",
            (dto.AdditionalRecipients ?? new List<string>())
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        target.LastUpdatedBy = updatedByEmail;
        target.LastUpdatedDateUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(target);
    }

    public async Task<int> SendTestAlertAsync(int organizationId, SendTestNotificationRequestDto dto)
    {
        var recipients = await ResolveRecipientsAsync(organizationId, dto.AdditionalRecipients);
        if (recipients.Count == 0) return 0;

        // Grab a real item from the org if available, or fall back to realistic sample data
        var sampleItem = await _context.InventoryItems
            .Include(i => i.Container)
                .ThenInclude(c => c!.Room)
                    .ThenInclude(r => r!.Location)
            .FirstOrDefaultAsync(i => i.OrganizationId == organizationId);

        var itemName = sampleItem?.Name ?? "Nitrile Exam Gloves (Box of 100)";
        var supplyRoom = sampleItem?.Container?.Room?.Name ?? "Sterilization & Prep Room A";
        var locationName = sampleItem?.Container?.Room?.Location?.Name ?? "Primary Clinic Hub";
        var currentQty = dto.TriggerType == TriggerOutOfStock ? "0" : (sampleItem?.Quantity.ToString() ?? "4");
        var minThreshold = sampleItem?.MinimumThreshold.ToString() ?? "10";

        var populatedMarkdown = ReplaceTokens(
            dto.TemplateMarkdown,
            itemName,
            supplyRoom,
            currentQty,
            minThreshold,
            locationName);

        var subject = $"[TEST] SupplySmart Alert: {dto.TriggerType}";
        await _emailService.SendAlertEmailAsync(recipients, subject, populatedMarkdown);
        return recipients.Count;
    }

    public async Task CheckAndTriggerItemStockAlertAsync(int organizationId, int previousQuantity, InventoryItem updatedItem)
    {
        try
        {
            string? triggeredEvent = null;

            // 1. Crossed into Out of Stock (0)
            if (previousQuantity > 0 && updatedItem.Quantity == 0)
            {
                triggeredEvent = TriggerOutOfStock;
            }
            // 2. Crossed into Low Stock (<= MinimumThreshold, but > 0)
            else if (previousQuantity > updatedItem.MinimumThreshold &&
                     updatedItem.Quantity <= updatedItem.MinimumThreshold &&
                     updatedItem.Quantity > 0)
            {
                triggeredEvent = TriggerLowStock;
            }

            if (triggeredEvent == null) return;

            var templates = await EnsureDefaultTemplatesAsync(organizationId);
            var template = templates.FirstOrDefault(t => t.TriggerType == triggeredEvent);
            if (template == null) return;

            // Load spatial hierarchy names if not already loaded
            var itemWithLocation = await _context.InventoryItems
                .Include(i => i.Container)
                    .ThenInclude(c => c!.Room)
                        .ThenInclude(r => r!.Location)
                .FirstOrDefaultAsync(i => i.Id == updatedItem.Id && i.OrganizationId == organizationId);

            var itemName = itemWithLocation?.Name ?? updatedItem.Name;
            var supplyRoom = itemWithLocation?.Container?.Room?.Name ?? "Supply Room";
            var locationName = itemWithLocation?.Container?.Room?.Location?.Name ?? "Main Hub";

            var extraEmails = SplitEmails(template.AdditionalRecipients);
            var recipients = await ResolveRecipientsAsync(organizationId, extraEmails);

            if (recipients.Count == 0) return;

            var body = ReplaceTokens(
                template.TemplateMarkdown,
                itemName,
                supplyRoom,
                updatedItem.Quantity.ToString(),
                updatedItem.MinimumThreshold.ToString(),
                locationName);

            var subject = triggeredEvent == TriggerOutOfStock
                ? $"❌ Out of Stock Alert: {itemName}"
                : $"🚨 Low Stock Alert: {itemName}";

            await _emailService.SendAlertEmailAsync(recipients, subject, body);
        }
        catch (Exception ex)
        {
            // Never let an email failure break an inventory update transaction
            _logger.LogError(ex, "Failed to dispatch automated stock alert email for item {ItemId}", updatedItem.Id);
        }
    }

    private async Task<List<NotificationTemplate>> EnsureDefaultTemplatesAsync(int organizationId)
    {
        var existing = await _context.NotificationTemplates
            .Where(t => t.OrganizationId == organizationId)
            .ToListAsync();

        var defaults = GetDefaultTemplates();
        var addedAny = false;

        foreach (var def in defaults)
        {
            if (!existing.Any(e => e.TriggerType == def.Key))
            {
                var newTemplate = new NotificationTemplate
                {
                    OrganizationId = organizationId,
                    TriggerType = def.Key,
                    TemplateMarkdown = def.Value,
                    AdditionalRecipients = string.Empty,
                    CreatedBy = "System Default",
                    LastUpdatedBy = "System Default",
                    CreatedDateUtc = DateTime.UtcNow,
                    LastUpdatedDateUtc = DateTime.UtcNow
                };
                _context.NotificationTemplates.Add(newTemplate);
                existing.Add(newTemplate);
                addedAny = true;
            }
        }

        if (addedAny)
        {
            await _context.SaveChangesAsync();
        }

        return existing;
    }

    private async Task<List<string>> ResolveRecipientsAsync(int organizationId, IEnumerable<string>? additionalEmails)
    {
        // Automatically fetch all active Admins and Managers in this tenant
        var adminAndManagerEmails = await _context.Users
            .Where(u => u.OrganizationId == organizationId &&
                        (u.Role == UserRole.Admin || u.Role == UserRole.Manager))
            .Select(u => u.Email)
            .ToListAsync();

        var combined = new HashSet<string>(adminAndManagerEmails, StringComparer.OrdinalIgnoreCase);

        if (additionalEmails != null)
        {
            foreach (var extra in additionalEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                combined.Add(extra.Trim());
            }
        }

        return combined.ToList();
    }

    private static string ReplaceTokens(
        string template,
        string itemName,
        string supplyRoom,
        string currentQty,
        string minThreshold,
        string locationName)
    {
        return template
            .Replace("{{item_name}}", itemName)
            .Replace("{{supply_room}}", supplyRoom)
            .Replace("{{current_quantity}}", currentQty)
            .Replace("{{minimum_threshold}}", minThreshold)
            .Replace("{{location_name}}", locationName);
    }

    private static Dictionary<string, string> GetDefaultTemplates() => new()
    {
        [TriggerLowStock] =
@"# 🚨 SupplySmart Low Stock Alert: {{item_name}}

**Location Hub:** {{location_name}}  
**Supply Room:** {{supply_room}}  
**Current Stock:** {{current_quantity}}  
**Minimum Threshold:** {{minimum_threshold}}

Please restock this item at your earliest convenience.",

        [TriggerOutOfStock] =
@"# ❌ CRITICAL: {{item_name}} is Out of Stock!

**Location Hub:** {{location_name}}  
**Supply Room:** {{supply_room}}  
**Current Stock:** {{current_quantity}} (Depleted)  
**Minimum Threshold:** {{minimum_threshold}}

Immediate restock is required to prevent procedural bottlenecks.",

        [TriggerWeeklyDigest] =
@"# 📊 SupplySmart Weekly Inventory Digest

**Primary Hub:** {{location_name}}  
**Featured Watch Item:** {{item_name}} ({{supply_room}})  
**Current Stock Level:** {{current_quantity}} units (Threshold: {{minimum_threshold}})

Review your full low-stock and expiring catalog items inside the SupplySmart dashboard."
    };

    private static List<string> SplitEmails(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim())
                  .Where(s => !string.IsNullOrWhiteSpace(s))
                  .ToList();
    }

    private static NotificationTemplateDto MapToDto(NotificationTemplate entity) => new()
    {
        Id = entity.Id,
        TriggerType = entity.TriggerType,
        TemplateMarkdown = entity.TemplateMarkdown,
        AdditionalRecipients = SplitEmails(entity.AdditionalRecipients),
        LastUpdatedBy = entity.LastUpdatedBy,
        LastUpdatedDateUtc = entity.LastUpdatedDateUtc
    };
}