using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class CurrentUserPermissionsTests
{
    [Fact]
    public async Task RemovedAdminCannotReuseTokenToReadOrInvite()
    {
        using var app = new SettingsApp();
        var owner = await app.SignIn(1);
        var removed = await app.SignIn(2);
        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync("/api/settings/users/2")).StatusCode);

        var login = await app.NewClient().PostAsJsonAsync("/api/auth/login",
            new { email = "user2@example.test", password = SettingsApp.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await removed.GetAsync("/api/settings/users/")).StatusCode);
        var invite = await removed.PostAsJsonAsync("/api/settings/users/invite",
            new { fullName = "Unauthorized invite", email = "blocked@example.test", role = "Staff" });
        Assert.Equal(HttpStatusCode.Unauthorized, invite.StatusCode);
        Assert.False(await app.ReadDb(db => db.Users.AnyAsync(u => u.Email == "blocked@example.test")));
        Assert.Empty(app.Email.Invitations);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Fact]
    public async Task DemotedAdminMustSignInAgainWithCurrentRole()
    {
        using var app = new SettingsApp();
        var stale = await app.SignIn(2);
        await app.ReadDb(async db =>
        {
            (await db.Users.SingleAsync(u => u.Id == 2)).Role = UserRole.Staff;
            return await db.SaveChangesAsync();
        });

        var body = new { fullName = "Unauthorized invite", email = "blocked@example.test", role = "Staff" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.PostAsJsonAsync("/api/settings/users/invite", body)).StatusCode);
        var current = await app.SignIn(2);
        Assert.Equal(HttpStatusCode.OK, (await current.GetAsync("/api/settings/users/")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await current.PostAsJsonAsync("/api/settings/users/invite", body)).StatusCode);
        Assert.Empty(app.Email.Invitations);
    }

    [Fact]
    public async Task ReassignedUserCannotKeepAccessToPreviousOrganization()
    {
        using var app = new SettingsApp();
        var stale = await app.SignIn(2);
        await app.ReadDb(async db =>
        {
            (await db.Users.SingleAsync(u => u.Id == 2)).OrganizationId = 2;
            return await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.GetAsync("/api/settings/users/")).StatusCode);
        var current = await app.SignIn(2);
        var users = await current.GetFromJsonAsync<List<UserDto>>("/api/settings/users/");
        Assert.Equal(new[] { 2, 5 }, users!.Select(u => u.Id).Order().ToArray());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task UnchangedAccountsRetainAuthenticatedReadAccess(int userId)
    {
        using var app = new SettingsApp();
        var client = await app.SignIn(userId);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings/users/")).StatusCode);
    }
}
