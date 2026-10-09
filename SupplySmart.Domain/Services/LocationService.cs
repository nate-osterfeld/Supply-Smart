using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationDto>> GetLocationsAsync(int organizationId);
        Task<IEnumerable<InventoryItemDto>> GetContainerItemsAsync(int organizationId, int? containerId);
    }

    public class LocationService : ILocationService
    {
        private readonly SupplySmartDbContext _context;

        public LocationService(SupplySmartDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LocationDto>> GetLocationsAsync(int organizationId)
        {
            var locations = await _context.Locations
                .Where(l => l.OrganizationId == organizationId)
                .Include(l => l.Rooms)
                .ThenInclude(r => r.Containers)
                .ToListAsync();

            return locations.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                IsPrimary = l.IsPrimary,
                Rooms = l.Rooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Containers = r.Containers.Select(c => new ContainerDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Kind = c.Kind
                    }).ToList()
                }).ToList()
            });
        }

        public async Task<IEnumerable<InventoryItemDto>> GetContainerItemsAsync(int organizationId, int? containerId)
        {
            var query = _context.InventoryItems
                .Where(i => i.OrganizationId == organizationId);

            if (containerId.HasValue)
            {
                query = query.Where(i => i.ContainerId == containerId.Value);
            }

            var items = await query.ToListAsync();

            return items.Select(i => new InventoryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Sku = i.Sku,
                LotNumber = i.LotNumber,
                ExpirationDate = i.ExpirationDate.ToString("MMM dd, yyyy"),
                Quantity = i.Quantity,
                Status = i.Quantity <= i.MinimumThreshold
                    ? "Low Stock"
                    : (i.ExpirationDate <= DateTime.UtcNow.AddDays(30) ? "Expiring Soon" : "Safe")
            });
        }
    }
}