using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Repositories;

namespace SupplySmart.Domain.Services
{
    public interface ITenantSeederService
    {
        Task SeedInitialTenantDataAsync(int organizationId, int defaultLocationId, string adminEmail, string adminName);
    }

    public class TenantSeederService : ITenantSeederService
    {
        private readonly IBaseRepository<Room> _roomRepo;
        private readonly IBaseRepository<Container> _containerRepo;
        private readonly IBaseRepository<InventoryItem> _itemRepo;
        private readonly IBaseRepository<AuditLog> _auditLogRepo;

        public TenantSeederService(
            IBaseRepository<Room> roomRepo,
            IBaseRepository<Container> containerRepo,
            IBaseRepository<InventoryItem> itemRepo,
            IBaseRepository<AuditLog> auditLogRepo)
        {
            _roomRepo = roomRepo;
            _containerRepo = containerRepo;
            _itemRepo = itemRepo;
            _auditLogRepo = auditLogRepo;
        }

        public async Task SeedInitialTenantDataAsync(int organizationId, int defaultLocationId, string adminEmail, string adminName)
        {
            // 1. Create Room / Storage Area level
            var storageRoom = new Room
            {
                OrganizationId = organizationId,
                LocationId = defaultLocationId,
                Name = "Main Storage Facility",
                Kind = "Facility",
                Detail = "Central inventory storage area"
            };
            await _roomRepo.AddAsync(storageRoom);

            // 2. Create Parent Container (Supply Cabinet A)
            var cabinetA = new Container
            {
                OrganizationId = organizationId,
                RoomId = storageRoom.Id,
                ParentContainerId = null,
                Name = "Supply Cabinet A",
                Kind = "Cabinet",
                Detail = "Main Storage Facility"
            };
            await _containerRepo.AddAsync(cabinetA);

            // 3. Create Child Container (Shelf 3 inside Cabinet A)
            var shelf3 = new Container
            {
                OrganizationId = organizationId,
                RoomId = storageRoom.Id,
                ParentContainerId = cabinetA.Id,
                Name = "Shelf 3",
                Kind = "Shelf",
                Detail = "Supply Cabinet A"
            };
            await _containerRepo.AddAsync(shelf3);

            // 4. Create Deepest Container (Bin #4 inside Shelf 3)
            var bin4 = new Container
            {
                OrganizationId = organizationId,
                RoomId = storageRoom.Id,
                ParentContainerId = shelf3.Id,
                Name = "Bin #4",
                Kind = "Bin",
                Detail = "Supply Cabinet A · Shelf 3"
            };
            await _containerRepo.AddAsync(bin4);

            // 5. Seed Inventory Items to Bin #4
            var sampleItems = new[]
            {
                new InventoryItem
                {
                    OrganizationId = organizationId,
                    ContainerId = bin4.Id,
                    Name = "Heavy-Duty Safety Gloves · Medium",
                    Sku = "GLV-SAF-M",
                    LotNumber = "LT-48392",
                    Category = "Disposables",
                    Type = ItemType.Consumable,
                    Quantity = 120,
                    MinimumThreshold = 20,
                    UnitCost = 14.50m,
                    ExpirationDate = DateTime.UtcNow.AddDays(180),
                    Status = "Safe"
                },
                new InventoryItem
                {
                    OrganizationId = organizationId,
                    ContainerId = bin4.Id,
                    Name = "Standard Disinfectant Wipes",
                    Sku = "WIP-DSN-500",
                    LotNumber = "LT-49018",
                    Category = "Consumables",
                    Type = ItemType.Consumable,
                    Quantity = 86,
                    MinimumThreshold = 15,
                    UnitCost = 22.00m,
                    ExpirationDate = DateTime.UtcNow.AddDays(200),
                    Status = "Safe"
                },
                new InventoryItem
                {
                    OrganizationId = organizationId,
                    ContainerId = bin4.Id,
                    Name = "Industrial Adhesive Tape Rolls",
                    Sku = "ADH-TAP-02",
                    LotNumber = "LT-47221",
                    Category = "Consumables",
                    Type = ItemType.Consumable,
                    Quantity = 24,
                    MinimumThreshold = 10,
                    UnitCost = 32.00m,
                    ExpirationDate = DateTime.UtcNow.AddDays(15),
                    Status = "Expiring Soon"
                },
                new InventoryItem
                {
                    OrganizationId = organizationId,
                    ContainerId = bin4.Id,
                    Name = "Surgical Metal Sterilization Tray",
                    Sku = "TRY-STR-01",
                    LotNumber = "LT-48110",
                    Category = "Equipment",
                    Type = ItemType.Reusable,
                    Quantity = 7,
                    MinimumThreshold = 10,
                    UnitCost = 85.00m,
                    ExpirationDate = DateTime.UtcNow.AddDays(365),
                    Status = "Low Stock"
                }
            };

            foreach (var item in sampleItems)
            {
                await _itemRepo.AddAsync(item);
            }

            // 6. Seed Initial Compliance Audit Log Entries
            var auditLogs = new[]
            {
                new AuditLog
                {
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    UserEmail = adminEmail,
                    UserName = adminName,
                    ActionType = "checkout",
                    Action = "Check-Out",
                    ItemDetails = "Maintenance Kit #SKU-4521",
                    LocationName = "Main Facility"
                },
                new AuditLog
                {
                    OrganizationId = organizationId,
                    Timestamp = DateTime.UtcNow.AddHours(-4),
                    UserEmail = adminEmail,
                    UserName = adminName,
                    ActionType = "restock",
                    Action = "Restock",
                    ItemDetails = "Shipping Packaging Boxes (Medium) - Qty: 25",
                    LocationName = "Main Facility"
                }
            };

            foreach (var log in auditLogs)
            {
                await _auditLogRepo.AddAsync(log);
            }
        }
    }
}