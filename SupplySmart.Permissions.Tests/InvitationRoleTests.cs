using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class InvitationRoleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Staf")]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("Admin, Manager")]
    public async Task InvalidRoleDoesNotCreateAnAccountOrSendMail(string? role)
    {
        using var app = new SettingsApp();
        var admin = await app.SignIn(1);
        var response = await admin.PostAsJsonAsync("/api/settings/users/invite",
            new { fullName = "Test invite", email = "new@example.test", role });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoInvitation(app);
    }

    [Fact]
    public async Task MissingRoleDoesNotSilentlyGrantManager()
    {
        using var app = new SettingsApp();
        var admin = await app.SignIn(1);
        var response = await admin.PostAsJsonAsync("/api/settings/users/invite",
            new { fullName = "Test invite", email = "new@example.test" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoInvitation(app);
    }

    [Theory]
    [InlineData("Admin", "Admin")]
    [InlineData("Manager", "Manager")]
    [InlineData("Staff", "Staff")]
    [InlineData("staff", "Staff")]
    [InlineData(" Manager ", "Manager")]
    public async Task ExplicitNamedRolesAreStoredAndCanSignIn(string input, string expected)
    {
        using var app = new SettingsApp();
        var admin = await app.SignIn(1);
        var response = await admin.PostAsJsonAsync("/api/settings/users/invite",
            new { fullName = "Test invite", email = "new@example.test", role = input });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, (await response.Content.ReadFromJsonAsync<UserDto>())!.Role);
        var invitation = Assert.Single(app.Email.Invitations);
        Assert.Equal("new@example.test", invitation.Email);
        var login = await app.NewClient().PostAsJsonAsync("/api/auth/login",
            new { email = invitation.Email, password = invitation.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(expected, (await login.Content.ReadFromJsonAsync<AuthResponseDto>())!.Role);
    }

    private static async Task AssertNoInvitation(SettingsApp app)
    {
        Assert.False(await app.ReadDb(db => db.Users.AnyAsync(u => u.Email == "new@example.test")));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
        Assert.Empty(app.Email.Invitations);
    }
}
