using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationDto>> GetLocationsAsync(int organizationId);
        Task<IEnumerable<InventoryItemDto>> GetContainerItemsAsync(int organizationId, int? containerId);
        Task<RoomDto> CreateRoomAsync(int organizationId, int locationId, string roomName);
        Task<ContainerDto> CreateContainerAsync(int organizationId, int roomId, string containerName, string kind, int? parentContainerId);
        Task<InventoryItemDto> AddItemAsync(int organizationId, int containerId, CreateItemDto dto, string userEmail);
        Task<bool> MoveItemAsync(int organizationId, int itemId, int targetContainerId, int? quantityToMove, int? minimumThreshold, string userEmail);
        Task<bool> DeleteContainerAsync(int organizationId, int containerId);
        Task<bool> DeleteRoomAsync(int organizationId, int roomId);
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
                        Kind = c.Kind,
                        ParentContainerId = c.ParentContainerId
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
                ExpirationDate = i.ExpirationDate.HasValue ? i.ExpirationDate.Value.ToString("MMM dd, yyyy") : "N/A",
                Quantity = i.Quantity,
                Status = i.Quantity <= i.MinimumThreshold
                    ? "Low Stock"
                    : (i.ExpirationDate <= DateTime.UtcNow.AddDays(30) ? "Expiring Soon" : "Safe"),
                Type = (int)i.Type
            });
        }

        public async Task<object> GetContainerMetricsAsync(int organizationId, int? containerId)
        {
            var query = _context.InventoryItems.Where(i => i.OrganizationId == organizationId);

            if (containerId.HasValue)
            {
                query = query.Where(i => i.ContainerId == containerId.Value);
            }

            var items = await query.ToListAsync();
            int totalItems = items.Count;
            int lowOrExpiring = items.Count(i => i.Quantity <= i.MinimumThreshold || (i.ExpirationDate.HasValue && i.ExpirationDate.Value <= DateTime.UtcNow.AddDays(30)));

            string healthStatus = "Good";
            if (totalItems > 0 && ((double)lowOrExpiring / totalItems) > 0.2)
            {
                healthStatus = "Needs Attention";
            }

            return new
            {
                CapacityPercentage = Math.Min(100, totalItems * 5), // Example capacity calculation based on item load
                OccupiedSlots = totalItems,
                MaxSlots = 20,
                InventoryHealth = healthStatus,
                HealthDetail = totalItems == 0 ? "No items in container" : $"{Math.Round((double)(totalItems - lowOrExpiring) / totalItems * 100)}% of items are within safe levels",
                LastCountDate = "Today",
                CountedBy = "System Admin"
            };
        }

        public async Task<RoomDto> CreateRoomAsync(int organizationId, int locationId, string roomName)
        {
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == locationId && l.OrganizationId == organizationId);

            if (location == null) throw new UnauthorizedAccessException("Invalid location scope.");

            var room = new Room
            {
                Name = roomName,
                LocationId = locationId,
                OrganizationId = organizationId
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return new RoomDto { Id = room.Id, Name = room.Name };
        }

        public async Task<ContainerDto> CreateContainerAsync(int organizationId, int roomId, string containerName, string kind, int? parentContainerId)
        {
            var room = await _context.Rooms
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Location.OrganizationId == organizationId);

            if (room == null) throw new UnauthorizedAccessException("Invalid room scope.");

            // Optional validation: Ensure parent container exists and belongs to the same room/org if specified
            if (parentContainerId.HasValue)
            {
                var parentContainer = await _context.Containers
                    .FirstOrDefaultAsync(c => c.Id == parentContainerId.Value && c.RoomId == roomId && c.OrganizationId == organizationId);
                if (parentContainer == null) throw new ArgumentException("Invalid parent container scope.");
            }

            var container = new Container
            {
                Name = containerName,
                Kind = kind,
                RoomId = roomId,
                OrganizationId = organizationId,
                ParentContainerId = parentContainerId
            };

            _context.Containers.Add(container);
            await _context.SaveChangesAsync();

            return new ContainerDto
            {
                Id = container.Id,
                Name = container.Name,
                Kind = container.Kind,
                ParentContainerId = container.ParentContainerId
            };
        }

        public async Task<InventoryItemDto> AddItemAsync(int organizationId, int containerId, CreateItemDto dto, string userEmail)
        {
            var container = await _context.Containers
                .Include(c => c.Room)
                .ThenInclude(r => r.Location)
                .FirstOrDefaultAsync(c => c.Id == containerId && c.Room.Location.OrganizationId == organizationId);

            if (container == null) throw new UnauthorizedAccessException("Invalid container scope.");

            var item = new InventoryItem
            {
                OrganizationId = organizationId,
                ContainerId = containerId,
                Name = dto.Name,
                Sku = dto.Sku,
                LotNumber = dto.LotNumber,
                Type = (ItemType) dto.Type,
                ExpirationDate = dto.Type == 1 ? null : dto.ExpirationDate,
                Quantity = dto.Quantity,
                MinimumThreshold = dto.MinimumThreshold,
                UnitCost = dto.UnitCost
            };

            _context.InventoryItems.Add(item);

            // Log to immutable audit trail
            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Item Added",
                ItemDetails = $"{dto.Name} (Qty: {dto.Quantity})",
                LocationName = $"{container.Room.Name} › {container.Name}"
            });

            await _context.SaveChangesAsync();

            return new InventoryItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Sku = item.Sku,
                LotNumber = item.LotNumber,
                ExpirationDate = item.ExpirationDate.HasValue ? item.ExpirationDate.Value.ToString("MMM dd, yyyy") : "N/A",
                Quantity = item.Quantity,
                Status = "Safe"
            };
        }

        public async Task<bool> MoveItemAsync(int organizationId, int itemId, int targetContainerId, int? quantityToMove, int? minimumThreshold, string userEmail)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Container)
                .ThenInclude(c => c.Room)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId);

            var targetContainer = await _context.Containers
                .Include(c => c.Room)
                .ThenInclude(r => r.Location)
                .FirstOrDefaultAsync(c => c.Id == targetContainerId && c.Room.Location.OrganizationId == organizationId);

            if (item == null || targetContainer == null) return false;

            // Determine quantity to move (default to full quantity if not provided)
            int moveQty = quantityToMove ?? item.Quantity;
            if (moveQty <= 0 || moveQty > item.Quantity) return false; // Invalid transfer amount

            var oldLocationContext = $"{item.Container.Room.Name} › {item.Container.Name}";
            var newLocationContext = $"{targetContainer.Room.Name} › {targetContainer.Name}";

            if (moveQty == item.Quantity)
            {
                // Scenario 1: Full Transfer (Relocate entire row)
                item.ContainerId = targetContainerId;

                // If a new threshold was specified during the move, update it
                if (minimumThreshold.HasValue)
                {
                    item.MinimumThreshold = minimumThreshold.Value;
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow,
                    UserEmail = userEmail,
                    ActionType = "Item Relocated (Full)",
                    ItemDetails = $"{item.Name} ({moveQty} units) moved from {oldLocationContext} to {newLocationContext}",
                    LocationName = newLocationContext
                });
            }
            else
            {
                // Scenario 2 & 3: Partial Transfer (Split quantity)
                item.Quantity -= moveQty;

                // Check if an identical item batch already exists in the target container
                var existingTargetItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.OrganizationId == organizationId
                        && i.ContainerId == targetContainerId
                        && i.Sku == item.Sku
                        && i.LotNumber == item.LotNumber
                        && i.ExpirationDate == item.ExpirationDate);

                if (existingTargetItem != null)
                {
                    // Scenario 3: Merge into existing destination batch
                    existingTargetItem.Quantity += moveQty;
                }
                else
                {
                    // Scenario 2: Create a new split batch record in the destination container
                    var splitItem = new InventoryItem
                    {
                        OrganizationId = organizationId,
                        ContainerId = targetContainerId,
                        Name = item.Name,
                        Sku = item.Sku,
                        LotNumber = item.LotNumber,
                        ExpirationDate = item.ExpirationDate,
                        Quantity = moveQty,
                        MinimumThreshold = minimumThreshold ?? item.MinimumThreshold, // Use provided threshold or fall back to source
                        UnitCost = item.UnitCost
                    };
                    _context.InventoryItems.Add(splitItem);
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow,
                    UserEmail = userEmail,
                    ActionType = "Item Relocated (Partial Split)",
                    ItemDetails = $"{item.Name} (Partial Qty: {moveQty}) moved from {oldLocationContext} to {newLocationContext}",
                    LocationName = newLocationContext
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteContainerAsync(int organizationId, int containerId)
        {
            var container = await _context.Containers
                .Include(c => c.Room)
                .ThenInclude(r => r.Location)
                .FirstOrDefaultAsync(c => c.Id == containerId && c.Room.Location.OrganizationId == organizationId);

            if (container == null) return false;

            _context.Containers.Remove(container);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomAsync(int organizationId, int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Location.OrganizationId == organizationId);

            if (room == null) return false;

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}