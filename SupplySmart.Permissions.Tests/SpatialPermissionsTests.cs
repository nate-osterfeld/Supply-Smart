using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Tests;

public class SpatialPermissionsTests
{
    private static readonly string[] Operations=["create-room","rename-room","delete-room","create-container","rename-container","delete-container"];
    public static IEnumerable<object[]> RoleCases => Operations.SelectMany(op => new[]{0,1,3,4}.Select(id=>new object[]{op,id}));
    public static IEnumerable<object[]> CrossClinicCases => Operations.SelectMany(op=>new[]{1,4}.Select(id=>new object[]{op,id}));

    [Theory, MemberData(nameof(RoleCases))]
    public async Task SpatialMutationsRequireAdminOrManager(string operation,int userId)
    {
        using var app=new SettingsApp();
        var client=userId==0 ? app.NewClient() : await app.SignIn(userId);
        await Seed(app);
        var before=await Snapshot(app);
        var response=await Send(client,operation,1);
        var expected=userId==0 ? HttpStatusCode.Unauthorized : userId==3 ? HttpStatusCode.Forbidden : HttpStatusCode.OK;
        Assert.Equal(expected,response.StatusCode);
        var after=await Snapshot(app);
        if(userId is 0 or 3) Assert.Equal(before,after);
        else Assert.NotEqual(before,after);
    }

    [Theory, MemberData(nameof(CrossClinicCases))]
    public async Task AllowedRoleCannotMutateAnotherClinic(string operation,int userId)
    {
        using var app=new SettingsApp();var client=await app.SignIn(userId);
        await Seed(app);var before=await Snapshot(app);
        var response=await Send(client,operation,2);
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        Assert.Equal(before,await Snapshot(app));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9999)]
    public async Task MissingAndForeignStorageParentsHaveTheSameResponse(int parentId)
    {
        using var app = new SettingsApp();
        using var manager = await app.SignIn(4);
        using var staff = await app.SignIn(3);
        await Seed(app);
        var before = await Snapshot(app);

        using var room = await Send(manager, "create-room", parentId);
        using var container = await Send(manager, "create-container", parentId);
        using var nested = await manager.PostAsJsonAsync("/api/locations/rooms/1/containers",
            new { name = "Nested bin", kind = "Bin", parentContainerId = parentId });
        using var item = await staff.PostAsJsonAsync($"/api/locations/containers/{parentId}/items",
            new { name = "New supply", sku = "TEST", quantity = 1 });

        foreach (var response in new[] { room, container, nested, item })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            Assert.Equal("Storage parent not found.", problem!.Title);
        }
        Assert.Equal(before, await Snapshot(app));
        Assert.Equal(0, await app.ReadDb(db => db.InventoryItems.CountAsync()));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Fact]
    public async Task StaffRetainsReadAccessAndCannotCreateLocations()
    {
        using var app=new SettingsApp();var staff=await app.SignIn(3);await Seed(app);
        Assert.Equal(HttpStatusCode.OK,(await staff.GetAsync("/api/locations/")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await staff.PostAsJsonAsync("/api/locations/",new {name="Unauthorized facility"})).StatusCode);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client,string operation,int id) => operation switch
    {
        "create-room" => client.PostAsJsonAsync($"/api/locations/rooms/{id}",new {name="New room"}),
        "rename-room" => client.PutAsJsonAsync($"/api/locations/rooms/{id}",new {name="Renamed room"}),
        "delete-room" => client.DeleteAsync($"/api/locations/rooms/{id}"),
        "create-container" => client.PostAsJsonAsync($"/api/locations/rooms/{id}/containers",new {name="New bin",kind="Bin"}),
        "rename-container" => client.PutAsJsonAsync($"/api/locations/containers/{id}",new {name="Renamed bin",kind="Bin"}),
        "delete-container" => client.DeleteAsync($"/api/locations/containers/{id}"),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
    private static Task<int> Seed(SettingsApp app) => app.ReadDb(async db=>
    {
        db.Locations.AddRange(new Location {Id=1,OrganizationId=1,Name="Own clinic"},new Location {Id=2,OrganizationId=2,Name="Other clinic"});
        db.Rooms.AddRange(new Room {Id=1,LocationId=1,OrganizationId=1,Name="Own room"},new Room {Id=2,LocationId=2,OrganizationId=2,Name="Other room"});
        db.Containers.AddRange(new Container {Id=1,RoomId=1,OrganizationId=1,Name="Own bin",Kind="Bin"},new Container {Id=2,RoomId=2,OrganizationId=2,Name="Other bin",Kind="Bin"});
        return await db.SaveChangesAsync();
    });
    private static Task<string> Snapshot(SettingsApp app) => app.ReadDb(async db=>
    {
        var rooms=await db.Rooms.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new {x.Id,x.Name,x.OrganizationId,x.LocationId}).ToListAsync();
        var bins=await db.Containers.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new {x.Id,x.Name,x.OrganizationId,x.RoomId,x.Kind}).ToListAsync();
        return System.Text.Json.JsonSerializer.Serialize(new {rooms,bins});
    });
}
