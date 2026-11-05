using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class UserPermissionsTests
{
    [Fact]
    public async Task OnlyAdminsCanReadOrChangeUsers()
    {
        using var app = new SettingsApp();
        using var anonymous = app.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/settings/users")).StatusCode);
        foreach (var id in new[] { 3, 4 })
        {
            using var member = await app.SignIn(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/settings/users")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync("/api/settings/users/2/role", new { role = "Staff" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync("/api/settings/users/2")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "New", email = "new@example.test", role = "Admin" })).StatusCode);
        }
    }

    [Fact]
    public async Task UsersAreScopedToTheAdministratorsOrganization()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        var users = await admin.GetFromJsonAsync<UserDto[]>("/api/settings/users");
        Assert.Equal(4, users!.Length);
        Assert.DoesNotContain(users, u => u.Id == 5);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/settings/users/5/role", new { role = "Staff" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync("/api/settings/users/5")).StatusCode);
    }

    [Fact]
    public async Task RoleChangesEndOldSessionsEvenIfTheRoleChangesBack()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        using var target = await app.SignIn(2);
        (await admin.PutAsJsonAsync("/api/settings/users/2/role", new { role = "Staff" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/auth/locations")).StatusCode);
        using var newSession = await app.SignIn(2);
        Assert.Equal(HttpStatusCode.OK, (await newSession.GetAsync("/api/auth/locations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await newSession.GetAsync("/api/settings/users")).StatusCode);
        (await admin.PutAsJsonAsync("/api/settings/users/2/role", new { role = "Admin" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/settings/users")).StatusCode);
        Assert.Equal(2, await app.ReadDb(db => db.AuditLogs.CountAsync(a => a.ActionType == "User Role Changed")));
    }

    [Fact]
    public async Task RevocationEndsTheUsersExistingSession()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        using var target = await app.SignIn(3);
        (await admin.DeleteAsync("/api/settings/users/3")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await target.GetAsync("/api/auth/locations")).StatusCode);
        Assert.False(await app.ReadDb(db => db.Users.AnyAsync(u => u.Id == 3)));
        Assert.Equal(1, await app.ReadDb(db => db.AuditLogs.CountAsync(a => a.ActionType == "User Revoked")));
    }

    [Fact]
    public async Task AdminCannotRemoveOrDemoteTheirOwnAccountIncludingTheLastAdmin()
    {
        using var app = new SettingsApp();
        foreach (var id in new[] { 1, 5 })
        {
            using var admin = await app.SignIn(id);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/settings/users/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/settings/users/{id}/role", new { role = "Staff" })).StatusCode);
        }
    }

    [Theory]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("Owner")]
    [InlineData("")]
    public async Task InvalidRolesAreRejected(string role)
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync("/api/settings/users/2/role", new { role })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "New", email = "new@example.test", role })).StatusCode);
        Assert.Empty(app.Email.Invitations);
    }

    [Fact]
    public async Task InvitationValidatesInputAndNormalizesEmail()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = " ", email = "bad", role = "Staff" })).StatusCode);
        var response = await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "  New member  ", email = " NEW@Example.test ", role = "Staff" });
        response.EnsureSuccessStatusCode();
        var added = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("new@example.test", added!.Email);
        Assert.Equal("New member", added.FullName);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "Duplicate", email = "NEW@example.test", role = "Staff" })).StatusCode);
        Assert.Single(app.Email.Invitations);
        using var login = app.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await login.PostAsJsonAsync("/api/auth/login", new { email = added.Email, password = app.Email.Invitations[0].Password })).StatusCode);
    }

    [Fact]
    public async Task FailedInvitationDoesNotLeaveAnAccountOrAuditEntry()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        app.Email.Fail = true;
        var response = await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "New", email = "new@example.test", role = "Staff" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(await app.ReadDb(db => db.Users.AnyAsync(u => u.Email == "new@example.test")));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        app.Email.Fail = false;
        (await admin.PostAsJsonAsync("/api/settings/users/invite", new { fullName = "New", email = "new@example.test", role = "Staff" })).EnsureSuccessStatusCode();
    }
}
