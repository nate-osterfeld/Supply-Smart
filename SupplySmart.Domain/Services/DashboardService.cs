using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models;

namespace SupplySmart.Domain.Services
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetMetricsAsync(int organizationId, int? locationId);
        Task<ProcedureForecastSummaryDto> GetForecastSummaryAsync(int organizationId);
        Task<IEnumerable<AuditLogEntryDto>> GetRecentAuditLogsAsync(int organizationId, int limit = 5);
    }

    public class DashboardService : IDashboardService
    {
        private const int ExpiringSoonDaysThreshold = 30;
        private readonly SupplySmartDbContext _context;

        public DashboardService(SupplySmartDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardMetricsDto> GetMetricsAsync(int organizationId, int? locationId)
        {
            // Base query scoped by tenant organization
            var query = _context.InventoryItems
                .Where(i => i.OrganizationId == organizationId);

            // Optional physical location filter via spatial room relation
            if (locationId.HasValue)
            {
                query = query.Where(i => i.Container.Room.LocationId == locationId.Value);
            }

            var items = await query.ToListAsync();
            var totalItemsCount = items.Count;
            var now = DateTime.UtcNow;

            int expiredItemsCount = items.Count(i => i.ExpirationDate.HasValue && i.ExpirationDate.Value <= now);
            int expiringSoonItemsCount = items.Count(i => i.ExpirationDate.HasValue && i.ExpirationDate.Value > now && i.ExpirationDate.Value <= now.AddDays(ExpiringSoonDaysThreshold));
            int safeItemsCount = totalItemsCount - expiredItemsCount - expiringSoonItemsCount;

            int expiredPct = totalItemsCount > 0 ? (int)Math.Round((double)expiredItemsCount / totalItemsCount * 100) : 0;
            int expiringPct = totalItemsCount > 0 ? (int)Math.Round((double)expiringSoonItemsCount / totalItemsCount * 100) : 0;
            int safePct = Math.Max(0, 100 - expiredPct - expiringPct);

            return new DashboardMetricsDto
            {
                TotalInventoryValue = items.Sum(i => i.Quantity * i.UnitCost),
                ExpiringSoonCount = items
                    .Where(i => i.ExpirationDate.HasValue && i.ExpirationDate.Value > now && i.ExpirationDate.Value <= now.AddDays(ExpiringSoonDaysThreshold))
                    .Sum(i => i.Quantity),
                ActiveLowStockAlertsCount = items
                    .Where(i => i.Quantity <= i.MinimumThreshold)
                    .Sum(i => i.Quantity),
                PendingRestocksCount = items.Count(i => i.IsRestockPending),
                SafeStockPercentage = safePct,
                ExpiringSoonPercentage = expiringPct,
                ExpiredPercentage = expiredPct
            };
        }

        public async Task<ProcedureForecastSummaryDto> GetForecastSummaryAsync(int organizationId)
        {
            var scheduledProcedures = 17;
            var requiredKits = 23;
            var availableKits = 15;
            var deficit = requiredKits > availableKits;

            return await Task.FromResult(new ProcedureForecastSummaryDto
            {
                ScheduledProceduresCount = scheduledProcedures,
                RequiredKitsCount = requiredKits,
                AvailableKitsCount = availableKits,
                IsDeficitDetected = deficit,
                DeficitMessage = $"Next week's schedule ({scheduledProcedures} Scheduled Procedures) requires {requiredKits} procedure kits; only {availableKits} are currently available in stock."
            });
        }

        public async Task<IEnumerable<AuditLogEntryDto>> GetRecentAuditLogsAsync(int organizationId, int limit = 5)
        {
            return await _context.AuditLogs
                .Where(a => a.OrganizationId == organizationId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new AuditLogEntryDto
                {
                    Id = a.Id,
                    Timestamp = a.Timestamp,
                    UserEmail = a.UserEmail,
                    ActionType = a.ActionType,
                    ItemDetails = a.ItemDetails,
                    LocationContext = a.LocationName
                })
                .ToListAsync();
        }
    }
}