using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class SessionInvalidationTests
{
    [Fact]
    public async Task PasswordReplacementRevokesPreviouslyIssuedTokens()
    {
        using var app = new SettingsApp();
        using var stale = await app.SignIn(1);
        await app.ReadDb(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == 1);
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "replacement-passphrase");
            return await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.GetAsync("/api/settings/users/")).StatusCode);
        using var client = app.NewClient();
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = "user1@example.test", password = SettingsApp.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        var freshLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = "user1@example.test", password = "replacement-passphrase" });
        Assert.Equal(HttpStatusCode.OK, freshLogin.StatusCode);
        var account = await freshLogin.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", account!.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoringTheOriginalRoleDoesNotReactivateAnOldToken(bool synchronousSave)
    {
        using var app = new SettingsApp();
        using var stale = await app.SignIn(2);
        foreach (var role in new[] { UserRole.Staff, UserRole.Admin })
        {
            await app.ReadDb(async db =>
            {
                (await db.Users.SingleAsync(u => u.Id == 2)).Role = role;
                return synchronousSave ? db.SaveChanges() : await db.SaveChangesAsync();
            });
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.GetAsync("/api/settings/users/")).StatusCode);
        using var fresh = await app.SignIn(2);
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Fact]
    public async Task DisplayNameChangesDoNotRevokeOtherwiseValidSessions()
    {
        using var app = new SettingsApp();
        using var current = await app.SignIn(1);
        await app.ReadDb(async db =>
        {
            (await db.Users.SingleAsync(u => u.Id == 1)).FullName = "Updated display name";
            return await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.OK, (await current.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RestoringTheOriginalEmailOrOrganizationDoesNotReactivateAnOldToken(bool changeEmail)
    {
        using var app = new SettingsApp();
        using var stale = await app.SignIn(2);
        foreach (var restore in new[] { false, true })
        {
            await app.ReadDb(async db =>
            {
                var user = await db.Users.SingleAsync(u => u.Id == 2);
                if (changeEmail) user.Email = restore ? "user2@example.test" : "changed@example.test";
                else user.OrganizationId = restore ? 1 : 2;
                return await db.SaveChangesAsync();
            });
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await stale.GetAsync("/api/settings/users/")).StatusCode);
        using var fresh = await app.SignIn(2);
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/settings/users/")).StatusCode);
    }
}
