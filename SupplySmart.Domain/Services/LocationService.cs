using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Domain.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<LocationDto>> GetLocationsAsync(int organizationId);
        Task<LocationDto> CreateLocationAsync(int organizationId, string name, bool isPrimary, string userEmail);
        Task<bool> UpdateLocationAsync(int organizationId, int locationId, string name, bool isPrimary, string userEmail);
        Task<bool> DeleteLocationAsync(int organizationId, int locationId, string userEmail);
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
        private readonly INotificationService _notificationService;

        public LocationService(SupplySmartDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
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

        public async Task<LocationDto> CreateLocationAsync(int organizationId, string name, bool isPrimary, string userEmail)
        {
            var location = new Location
            {
                OrganizationId = organizationId,
                Name = name,
                IsPrimary = isPrimary
            };

            _context.Locations.Add(location);

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Location Created",
                ItemDetails = $"New facility location '{name}' provisioned",
                LocationName = name
            });

            await _context.SaveChangesAsync();

            return new LocationDto
            {
                Id = location.Id,
                Name = location.Name,
                IsPrimary = location.IsPrimary,
                Rooms = new List<RoomDto>()
            };
        }

        public async Task<bool> UpdateLocationAsync(int organizationId, int locationId, string name, bool isPrimary, string userEmail)
        {
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == locationId && l.OrganizationId == organizationId);

            if (location == null) return false;

            var oldName = location.Name;
            location.Name = name;
            location.IsPrimary = isPrimary;

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Location Updated",
                ItemDetails = $"Location '{oldName}' updated to '{name}'",
                LocationName = name
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLocationAsync(int organizationId, int locationId, string userEmail)
        {
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == locationId && l.OrganizationId == organizationId);

            if (location == null) return false;

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Location Deleted",
                ItemDetails = $"Location '{location.Name}' removed",
                LocationName = location.Name
            });

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            return true;
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
                    Category = !string.IsNullOrEmpty(i.Category) ? i.Category : "General",
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

            var org = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null) throw new InvalidOperationException("Organization not found.");

            var maxRoomsQuota = org.MaxRoomsQuota > 0 ? org.MaxRoomsQuota : 2;
            var planName = string.IsNullOrEmpty(org.SubscriptionPlan) ? "Free" : org.SubscriptionPlan;

            var currentRoomsCount = await _context.Rooms
                .CountAsync(r => r.OrganizationId == organizationId);

            if (currentRoomsCount >= maxRoomsQuota)
            {
                throw new InvalidOperationException(
                    $"You have reached the maximum room limit ({maxRoomsQuota} rooms) for your current {planName} Plan. Please upgrade your subscription in Workspace Settings to provision additional rooms.");
            }

            var room = new Room
            {
                Name = roomName,
                LocationId = locationId,
                OrganizationId = organizationId,
                Kind = "Room",
                Detail = string.Empty
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

            var org = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org == null) throw new InvalidOperationException("Organization not found.");

            var maxItemsQuota = org.MaxItemsQuota > 0 ? org.MaxItemsQuota : 50;
            var planName = string.IsNullOrEmpty(org.SubscriptionPlan) ? "Free" : org.SubscriptionPlan;

            var currentItemsCount = await _context.InventoryItems
                .CountAsync(i => i.OrganizationId == organizationId);

            if (currentItemsCount >= maxItemsQuota)
            {
                throw new InvalidOperationException(
                    $"You have reached the maximum inventory item limit ({maxItemsQuota} items) for your current {planName} Plan. Please upgrade your subscription in Workspace Settings to add more items.");
            }

            var item = new InventoryItem
            {
                OrganizationId = organizationId,
                ContainerId = containerId,
                Name = dto.Name,
                Sku = dto.Sku,
                Category = !string.IsNullOrEmpty(dto.Category) ? dto.Category : "General",
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

            // Check if newly added item starts out below threshold or out of stock
            await _notificationService.CheckAndTriggerItemStockAlertAsync(organizationId, dto.Quantity + 1, item);

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
                Category = item.Category,
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

            int oldQty = item.Quantity;
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

            // Trigger stock alert if partial transfer leaves the source bin running low
            if (moveQty < oldQty)
            {
                await _notificationService.CheckAndTriggerItemStockAlertAsync(organizationId, oldQty, item);
            }

            return true;
        }

        public async Task<bool> UpdateItemAsync(int organizationId, int itemId, UpdateItemDto dto, string userEmail)
        {
            var item = await _context.InventoryItems
                .Include(i => i.Container)
                .ThenInclude(c => c.Room)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OrganizationId == organizationId);

            if (item == null)
            {
                return false;
            }

            // Save quantity before change for forecasting and alerts
            var quantityBefore = item.Quantity;
            var quantityAfter = dto.Quantity;
            var quantityChange = quantityAfter - quantityBefore;

            var historicalItemName = item.Name;
            var historicalSku = item.Sku;

            string? transactionType = null;
            if (quantityChange < 0)
            {
                transactionType = "checkout";
            }
            else if (quantityChange > 0)
            {
                transactionType = "restock";
            }

            // Update item fields
            item.Name = dto.Name;
            item.Sku = dto.Sku;
            item.LotNumber = dto.LotNumber;
            item.Type = (ItemType)dto.Type;
            item.ExpirationDate = dto.Type == 1 ? null : dto.ExpirationDate;
            item.Quantity = dto.Quantity;
            item.MinimumThreshold = dto.MinimumThreshold;
            item.UnitCost = dto.UnitCost;
            item.Category = !string.IsNullOrEmpty(dto.Category) ? dto.Category : "General";

            var locationContext = item.Container != null
                ? $"{item.Container.Room.Name} › {item.Container.Name}"
                : "Unknown Location";

            _context.AuditLogs.Add(new AuditLog
            {
                OrganizationId = organizationId,
                Timestamp = DateTime.UtcNow,
                UserEmail = userEmail,
                ActionType = "Item Updated",
                ItemDetails = $"{item.Name} (Qty: {item.Quantity}, SKU: {item.Sku})",
                LocationName = locationContext
            });

            if (transactionType != null)
            {
                _context.InventoryTransactions.Add(
                    new InventoryTransaction
                    {
                        OrganizationId = organizationId,
                        InventoryItemId = item.Id,
                        TransactionType = transactionType,
                        QuantityChange = quantityChange,
                        QuantityBefore = quantityBefore,
                        QuantityAfter = quantityAfter,
                        Timestamp = DateTime.UtcNow,
                        UserEmail = userEmail,
                        ItemName = historicalItemName,
                        Sku = historicalSku,
                        Notes = transactionType == "checkout"
                            ? $"Inventory decreased by {Math.Abs(quantityChange)} units."
                            : $"Inventory increased by {quantityChange} units."
                    }
                );
            }

            await _context.SaveChangesAsync();

            // Check and trigger email notifications if quantity crossed into low stock or out of stock
            await _notificationService.CheckAndTriggerItemStockAlertAsync(organizationId, quantityBefore, item);

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