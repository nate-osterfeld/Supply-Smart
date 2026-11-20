using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class AuthInputValidationTests
{
    public static TheoryData<string, object?> InvalidRegistrationFields => new()
    {
        { "password", null }, { "password", "" }, { "password", "short" },
        { "password", "            " }, { "password", new string('p', 1025) },
        { "email", null }, { "email", "not-an-email" }, { "email", new string('e', 250) + "@example.test" },
        { "organizationName", "" }, { "organizationName", new string('o', 201) },
        { "adminFullName", null }, { "adminFullName", new string('n', 151) }
    };

    [Theory]
    [MemberData(nameof(InvalidRegistrationFields))]
    public async Task InvalidRegistrationCannotCreateAnAccountOrPartialTenant(string field, object? value)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var input = Registration();
        input[field] = value;
        var response = await client.PostAsJsonAsync("/api/auth/register", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2, await app.ReadDb(db => db.Organizations.CountAsync()));
        Assert.Equal(5, await app.ReadDb(db => db.Users.CountAsync()));
        Assert.Equal(0, await app.ReadDb(db => db.Locations.CountAsync()));
        Assert.Equal(0, await app.ReadDb(db => db.AuditLogs.CountAsync()));
    }

    [Theory]
    [InlineData("user1@example.test")]
    [InlineData("missing@example.test")]
    public async Task NullPasswordCannotRevealWhetherAnAccountExists(string email)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("token", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, "valid-password")]
    [InlineData("not-an-email", "valid-password")]
    [InlineData("user1@example.test", "")]
    public async Task MalformedLoginIsRejectedBeforeAuthentication(string? email, string password)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NewPassphrasesWorkAndExistingShortPasswordsRemainUsable()
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal("Admin", account!.Role);
        Assert.False(string.IsNullOrWhiteSpace(account.Token));
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "new@example.test", password = "a passphrase with spaces" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        await app.ReadDb(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == 1);
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "old");
            return await db.SaveChangesAsync();
        });
        var legacy = await client.PostAsJsonAsync("/api/auth/login", new { email = "user1@example.test", password = "old" });
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
    }

    private static Dictionary<string, object?> Registration() => new()
    {
        ["organizationName"] = "New clinic", ["adminFullName"] = "New owner",
        ["email"] = "new@example.test", ["password"] = "a passphrase with spaces"
    };
}
