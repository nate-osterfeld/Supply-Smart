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

            return new DashboardMetricsDto
            {
                TotalInventoryValue = items.Sum(i => i.Quantity * i.UnitCost),
                ExpiringSoonCount = items.Count(i => i.ExpirationDate <= DateTime.UtcNow.AddDays(ExpiringSoonDaysThreshold) && i.ExpirationDate > DateTime.UtcNow),
                ActiveLowStockAlertsCount = items.Count(i => i.Quantity <= i.MinimumThreshold),
                PendingRestocksCount = items.Count(i => i.IsRestockPending)
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