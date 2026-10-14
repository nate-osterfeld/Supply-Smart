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
        Task<bool> UpdateRoomAsync(int organizationId, int roomId, string roomName, string userEmail);
        Task<bool> DeleteRoomAsync(int organizationId, int roomId);
        Task<ContainerDto> CreateContainerAsync(int organizationId, int roomId, string containerName, string kind, int? parentContainerId);
        Task<bool> UpdateContainerAsync(int organizationId, int containerId, string containerName, string kind, string userEmail);
        Task<bool> DeleteContainerAsync(int organizationId, int containerId);
        Task<InventoryItemDto> AddItemAsync(int organizationId, int containerId, CreateItemDto dto, string userEmail);
        Task<bool> MoveItemAsync(int organizationId, int itemId, int targetContainerId, int? quantityToMove, int? minimumThreshold, string userEmail);
        Task<bool> UpdateItemAsync(int organizationId, int itemId, UpdateItemDto dto, string userEmail);
        Task<bool> DeleteItemAsync(int organizationId, int itemId, string userEmail);
        
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

            return items.Select(i =>
            {
                var statuses = new List<string>();

                if (i.Quantity == 0)
                {
                    statuses.Add("Out of Stock");
                }
                else if (i.Quantity <= i.MinimumThreshold)
                {
                    statuses.Add("Low Stock");
                }

                if (i.ExpirationDate.HasValue)
                {
                    if (i.ExpirationDate.Value <= DateTime.UtcNow)
                    {
                        statuses.Add("Expired");
                    }
                    else if (i.ExpirationDate.Value <= DateTime.UtcNow.AddDays(30))
                    {
                        statuses.Add("Expiring Soon");
                    }
                }

                if (!statuses.Any())
                {
                    statuses.Add("Safe");
                }

                return new InventoryItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Sku = i.Sku,
                    LotNumber = i.LotNumber,
                    ExpirationDate = i.ExpirationDate.HasValue ? i.ExpirationDate.Value.ToString("MMM dd, yyyy") : "N/A",
                    Quantity = i.Quantity,
                    MinimumThreshold = i.MinimumThreshold,
                    UnitCost = i.UnitCost,
                    Statuses = statuses,
                    Type = (int)i.Type
                };
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
                CapacityPercentage = Math.Min(100, totalItems * 5),
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

        public async Task<bool> UpdateRoomAsync(int organizationId, int roomId, string roomName, string userEmail)
        {
            var room = await _context.Rooms
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Location.OrganizationId == organizationId);

            if (room == null) return false;

            var oldName = room.Name;
            room.Name = roomName;

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Room Updated",
                ItemDetails = $"Room renamed from '{oldName}' to '{roomName}'",
                LocationName = room.Name
            });

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

        public async Task<ContainerDto> CreateContainerAsync(int organizationId, int roomId, string containerName, string kind, int? parentContainerId)
        {
            var room = await _context.Rooms
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == roomId && r.Location.OrganizationId == organizationId);

            if (room == null) throw new UnauthorizedAccessException("Invalid room scope.");

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

        public async Task<bool> UpdateContainerAsync(int organizationId, int containerId, string containerName, string kind, string userEmail)
        {
            var container = await _context.Containers
                .Include(c => c.Room)
                .ThenInclude(r => r.Location)
                .FirstOrDefaultAsync(c => c.Id == containerId && c.Room.Location.OrganizationId == organizationId);

            if (container == null) return false;

            var oldName = container.Name;
            container.Name = containerName;
            container.Kind = kind;

            var locationContext = $"{container.Room.Name} › {container.Name}";

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Container Updated",
                ItemDetails = $"Container '{oldName}' updated to '{containerName}' ({kind})",
                LocationName = locationContext
            });

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
                Type = (ItemType)dto.Type,
                ExpirationDate = dto.Type == 1 ? null : dto.ExpirationDate,
                Quantity = dto.Quantity,
                MinimumThreshold = dto.MinimumThreshold,
                UnitCost = dto.UnitCost
            };

            _context.InventoryItems.Add(item);

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
                MinimumThreshold = item.MinimumThreshold,
                UnitCost = item.UnitCost,
                Statuses = new List<string> { "Safe" },
                Type = (int)item.Type
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

            int moveQty = quantityToMove ?? item.Quantity;
            if (moveQty <= 0 || moveQty > item.Quantity) return false;

            var oldLocationContext = $"{item.Container.Room.Name} › {item.Container.Name}";
            var newLocationContext = $"{targetContainer.Room.Name} › {targetContainer.Name}";

            if (moveQty == item.Quantity)
            {
                item.ContainerId = targetContainerId;

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
                item.Quantity -= moveQty;

                var existingTargetItem = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.OrganizationId == organizationId
                        && i.ContainerId == targetContainerId
                        && i.Sku == item.Sku
                        && i.LotNumber == item.LotNumber
                        && i.ExpirationDate == item.ExpirationDate);

                if (existingTargetItem != null)
                {
                    existingTargetItem.Quantity += moveQty;
                }
                else
                {
                    var splitItem = new InventoryItem
                    {
                        OrganizationId = organizationId,
                        ContainerId = targetContainerId,
                        Name = item.Name,
                        Sku = item.Sku,
                        LotNumber = item.LotNumber,
                        ExpirationDate = item.ExpirationDate,
                        Quantity = moveQty,
                        MinimumThreshold = minimumThreshold ?? item.MinimumThreshold,
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

        public async Task<bool> UpdateItemAsync(int organizationId, int itemId, UpdateItemDto dto, string userEmail)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Container)
                .ThenInclude(c => c.Room)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId);

            if (item == null) return false;

            item.Name = dto.Name;
            item.Sku = dto.Sku;
            item.LotNumber = dto.LotNumber;
            item.Type = (ItemType)dto.Type;
            item.ExpirationDate = dto.Type == 1 ? null : dto.ExpirationDate;
            item.Quantity = dto.Quantity;
            item.MinimumThreshold = dto.MinimumThreshold;
            item.UnitCost = dto.UnitCost;

            var locationContext = item.Container != null ? $"{item.Container.Room.Name} › {item.Container.Name}" : "Unknown Location";

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Item Updated",
                ItemDetails = $"{item.Name} (Qty: {item.Quantity}, SKU: {item.Sku})",
                LocationName = locationContext
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteItemAsync(int organizationId, int itemId, string userEmail)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Container)
                .ThenInclude(c => c.Room)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId);

            if (item == null) return false;

            var locationContext = item.Container != null ? $"{item.Container.Room.Name} › {item.Container.Name}" : "Unknown Location";

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Item Deleted",
                ItemDetails = $"{item.Name} (SKU: {item.Sku}) removed from inventory",
                LocationName = locationContext
            });

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}