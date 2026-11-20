using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class CatalogClinicScopeTests
{
    [Fact]
    public async Task CatalogStorageAndItemHistoryOnlyUseLocationsWithinTheClinic()
    {
        using var app = new SettingsApp();
        using var staff = await app.SignIn(3);
        using var other = await app.SignIn(5);
        await InventoryTestData.Seed(app, 3);
        await app.ReadDb(async db =>
        {
            // Existing records can have individually valid foreign keys that belong to different clinics.
            db.Rooms.Add(new Room { Id = 3, OrganizationId = 1, LocationId = 2, Name = "Mixed room" });
            db.Containers.AddRange(
                new Container { Id = 4, OrganizationId = 2, RoomId = 1, Name = "Foreign bin in local room" },
                new Container { Id = 5, OrganizationId = 1, RoomId = 2, Name = "Bin in foreign room" },
                new Container { Id = 6, OrganizationId = 1, RoomId = 3, Name = "Bin in foreign location" },
                new Container { Id = 7, OrganizationId = 1, RoomId = 1, ParentContainerId = 3, Name = "Local bin" });
            (await db.Containers.SingleAsync(c => c.Id == 2)).ParentContainerId = 1;
            foreach (var bin in new[] { 3, 4, 5, 6 })
                db.InventoryItems.Add(new InventoryItem { Id = 10 + bin, OrganizationId = 1, ContainerId = bin, Name = "Mixed", Quantity = 2 });
            db.InventoryItems.Add(new InventoryItem { Id = 20, OrganizationId = 2, ContainerId = 3, Name = "Other clinic's item" });
            return await db.SaveChangesAsync();
        });

        var catalog = await staff.GetFromJsonAsync<MasterCatalogItemDto[]>("/api/catalog?organizationId=2");
        Assert.NotNull(catalog);
        Assert.Equal(new[] { 1, 2 }, catalog.Select(i => i.Id).Order());
        Assert.All(catalog, i => Assert.StartsWith("Own room / ", i.Location));
        var otherCatalog = await other.GetFromJsonAsync<MasterCatalogItemDto[]>("/api/catalog");
        Assert.Equal(20, Assert.Single(otherCatalog!).Id);
        var stock = await staff.GetFromJsonAsync<InventoryItemDto[]>("/api/locations/items");
        Assert.Equal(new[] { 1, 2 }, stock!.Select(i => i.Id).Order());
        var layout = await staff.GetFromJsonAsync<LocationDto[]>("/api/locations");
        var room = Assert.Single(Assert.Single(layout!).Rooms);
        Assert.Equal(new[] { 1, 2, 7 }, room.Containers.Select(c => c.Id).Order());
        Assert.Equal(1, room.Containers.Single(c => c.Id == 2).ParentContainerId);
        Assert.Null(room.Containers.Single(c => c.Id == 7).ParentContainerId);

        using var edit = await staff.PutAsJsonAsync("/api/locations/items/13", new UpdateItemDto { Name = "Changed", Quantity = 1 });
        using var deletion = await staff.DeleteAsync("/api/locations/items/14");
        using var move = await staff.PostAsJsonAsync("/api/locations/items/1/move", new MoveItemDto { TargetContainerId = 4 });
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deletion.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, move.StatusCode);
        using var receipt = await staff.PostAsJsonAsync("/api/locations/containers/4/items", new CreateItemDto { Name = "Receipt" });
        Assert.False(receipt.IsSuccessStatusCode);
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Equal(7, await app.ReadDb(db => db.InventoryItems.CountAsync()));
    }
}
