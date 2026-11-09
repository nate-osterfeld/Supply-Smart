using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services;

public interface ISubscriptionService
{
    Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(int organizationId);
    Task<SubscriptionCheckoutResponseDto> UpdateSubscriptionTierAsync(int organizationId, string targetPlan, string userEmail);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly SupplySmartDbContext _context;
    private readonly IConfiguration _configuration;

    public SubscriptionService(SupplySmartDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<SubscriptionDetailsDto> GetSubscriptionDetailsAsync(int organizationId)
    {
        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        var usedRoomsCount = await _context.Rooms
            .CountAsync(r => r.OrganizationId == organizationId);

        var usedItemsCount = await _context.InventoryItems
            .CountAsync(i => i.OrganizationId == organizationId);

        var isStripeConfigured = !string.IsNullOrWhiteSpace(_configuration["StripeSettings:SecretKey"]);

        return new SubscriptionDetailsDto
        {
            PlanName = string.IsNullOrWhiteSpace(org.SubscriptionPlan) ? "Free" : org.SubscriptionPlan,
            Status = string.IsNullOrWhiteSpace(org.SubscriptionStatus) ? "Active" : org.SubscriptionStatus,
            BillingInterval = string.IsNullOrWhiteSpace(org.BillingInterval) ? "Annual" : org.BillingInterval,
            RenewalDateUtc = org.RenewalDateUtc == default ? DateTime.UtcNow.AddYears(1) : org.RenewalDateUtc,
            UsedRoomsCount = usedRoomsCount,
            MaxRoomsQuota = org.MaxRoomsQuota > 0 ? org.MaxRoomsQuota : 2,
            UsedItemsCount = usedItemsCount,
            MaxItemsQuota = org.MaxItemsQuota > 0 ? org.MaxItemsQuota : 50,
            IsStripeConfigured = isStripeConfigured
        };
    }

    public async Task<SubscriptionCheckoutResponseDto> UpdateSubscriptionTierAsync(int organizationId, string targetPlan, string userEmail)
    {
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org == null)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        var normalizedPlan = targetPlan?.Trim() switch
        {
            var p when string.Equals(p, "Free", StringComparison.OrdinalIgnoreCase) => "Free",
            var p when string.Equals(p, "Starter", StringComparison.OrdinalIgnoreCase) => "Starter",
            var p when string.Equals(p, "Professional", StringComparison.OrdinalIgnoreCase) => "Professional",
            var p when string.Equals(p, "Enterprise", StringComparison.OrdinalIgnoreCase) => "Enterprise",
            _ => throw new ArgumentException("Invalid subscription plan selected. Choose Free, Starter, Professional, or Enterprise.")
        };

        var (targetRoomsQuota, targetItemsQuota) = normalizedPlan switch
        {
            "Free" => (2, 50),
            "Starter" => (5, 250),
            "Professional" => (15, 1500),
            "Enterprise" => (9999, 999999),
            _ => (2, 50)
        };

        var currentRoomsCount = await _context.Rooms
            .CountAsync(r => r.OrganizationId == organizationId);

        var currentItemsCount = await _context.InventoryItems
            .CountAsync(i => i.OrganizationId == organizationId);

        if (currentRoomsCount > targetRoomsQuota)
        {
            throw new InvalidOperationException(
                $"Cannot switch to the {normalizedPlan} Plan ({targetRoomsQuota} rooms max) because your workspace currently has {currentRoomsCount} active supply rooms. Please remove {currentRoomsCount - targetRoomsQuota} room(s) first.");
        }

        if (currentItemsCount > targetItemsQuota)
        {
            throw new InvalidOperationException(
                $"Cannot switch to the {normalizedPlan} Plan ({targetItemsQuota} items max) because your workspace currently tracks {currentItemsCount} inventory items. Please remove {currentItemsCount - targetItemsQuota} item(s) first.");
        }

        org.SubscriptionPlan = normalizedPlan;
        org.SubscriptionStatus = "Active";
        org.MaxRoomsQuota = targetRoomsQuota;
        org.MaxItemsQuota = targetItemsQuota;
        if (org.RenewalDateUtc == default || org.RenewalDateUtc < DateTime.UtcNow)
        {
            org.RenewalDateUtc = DateTime.UtcNow.AddYears(1);
        }
        org.LastUpdatedDateUtc = DateTime.UtcNow;
        org.LastUpdatedBy = userEmail;

        var roomsDisplay = targetRoomsQuota == 9999 ? "Unlimited" : targetRoomsQuota.ToString();
        var itemsDisplay = targetItemsQuota == 999999 ? "Unlimited" : targetItemsQuota.ToString();

        _context.AuditLogs.Add(new AuditLog
        {
            OrganizationId = organizationId,
            Timestamp = DateTime.UtcNow,
            UserEmail = userEmail,
            ActionType = "Subscription Update",
            ItemDetails = $"Updated SaaS plan to {normalizedPlan} Plan ({roomsDisplay} rooms, {itemsDisplay} items quota)",
            LocationName = "Workspace Settings"
        });

        await _context.SaveChangesAsync();

        return new SubscriptionCheckoutResponseDto
        {
            UpdatedImmediately = true,
            CheckoutUrl = null,
            Message = $"Successfully updated workspace subscription to the {normalizedPlan} Plan."
        };
    }
}