using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services
{
    public interface ICatalogService
    {
        Task<IEnumerable<MasterCatalogItemDto>> GetMasterCatalogAsync(int organizationId);
        Task<IEnumerable<AuditLogEntryDto>> GetAuditLogsAsync(int organizationId);
    }

    public class CatalogService : ICatalogService
    {
        private readonly SupplySmartDbContext _context;

        public CatalogService(SupplySmartDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MasterCatalogItemDto>> GetMasterCatalogAsync(int organizationId)
        {
            var items = await _context.InventoryItems
                .Include(i => i.Container)
                .ThenInclude(c => c.Room)
                .Where(i => i.OrganizationId == organizationId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            return items.Select(i =>
            {
                var statuses = StockStatus.ForItem(i, now);
                string primaryStatus = statuses.Contains("Out of Stock") ? "Out of Stock" :
                                       statuses.Contains("Expired") ? "Expired" :
                                       statuses.Contains("Expiring Soon") ? "Expiring Soon" :
                                       statuses.Contains("Low Stock") ? "Low Stock" : "In Stock";

                return new MasterCatalogItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Sku = i.Sku,
                    Category = !string.IsNullOrEmpty(i.Category) ? i.Category : "General",
                    Quantity = i.Quantity,
                    Location = i.Container != null ? $"{i.Container.Room.Name} / {i.Container.Name}" : "Unassigned",
                    ExpirationDate = i.ExpirationDate.HasValue ? i.ExpirationDate.Value.ToString("MMM dd, yyyy") : "N/A",
                    Status = primaryStatus,
                    LotNumber = i.LotNumber,
                    MinimumThreshold = i.MinimumThreshold,
                    UnitCost = i.UnitCost,
                    Type = (int)i.Type
                };
            });
        }

        public async Task<IEnumerable<AuditLogEntryDto>> GetAuditLogsAsync(int organizationId)
        {
            var logs = await _context.AuditLogs
                .Where(a => a.OrganizationId == organizationId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            return logs.Select(a => new AuditLogEntryDto
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                UserEmail = a.UserEmail,
                ActionType = a.ActionType,
                ItemDetails = a.ItemDetails,
                LocationContext = a.LocationName
            });
        }
    }
}