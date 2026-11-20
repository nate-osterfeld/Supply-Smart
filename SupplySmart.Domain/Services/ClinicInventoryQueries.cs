using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Domain.Services;

internal static class ClinicInventoryQueries
{
    public static IQueryable<Container> ContainersInClinic(this SupplySmartDbContext db, int clinicId) =>
        db.Containers.Where(bin => bin.OrganizationId == clinicId && bin.Room.OrganizationId == clinicId &&
                                   bin.Room.Location.OrganizationId == clinicId);

    public static IQueryable<InventoryItem> ItemsInClinicStorage(this SupplySmartDbContext db, int clinicId)
    {
        var bins = db.ContainersInClinic(clinicId).Select(bin => bin.Id);
        return db.InventoryItems.Where(item => item.OrganizationId == clinicId && bins.Contains(item.ContainerId));
    }
}
