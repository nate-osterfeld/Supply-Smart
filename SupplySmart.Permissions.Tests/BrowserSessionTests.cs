using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SupplySmart.Endpoints.Security;

namespace SupplySmart.Tests;

public class BrowserSessionTests
{
    private const string CsrfHeader = "X-SupplySmart-CSRF";
    private const string Origin = "https://localhost";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrowserCredentialsCreateAPrivateCookieAndReturnOnlyProfileData(bool register)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var response = await Start(client, register);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var cookie = Assert.Single(SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()),
            item => item.Name.Value == BrowserSession.CookieName);
        Assert.True(cookie.HttpOnly);
        Assert.True(cookie.Secure);
        Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
        Assert.Equal(TimeSpan.FromDays(7), cookie.MaxAge);
        var signedExpiry = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(cookie.Value.Value).ValidTo;
        Assert.InRange(signedExpiry - DateTime.UtcNow, TimeSpan.FromDays(7) - TimeSpan.FromMinutes(1), TimeSpan.FromDays(7));
        Assert.Equal("/", cookie.Path.Value);
        Assert.False(cookie.Domain.HasValue);
        var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(profile.TryGetProperty("token", out _));
        Assert.Equal("Admin", profile.GetProperty("role").GetString());

        var state = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.False(state.GetProperty("user").TryGetProperty("token", out _));
        Assert.Equal(profile.GetProperty("email").GetString(), state.GetProperty("user").GetProperty("email").GetString());
        var csrf = state.GetProperty("csrfToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(csrf));
        Assert.DoesNotContain(cookie.Value.Value!, state.GetRawText());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings/users/")).StatusCode);

        var logout = await Write(client, HttpMethod.Post, "/api/auth/logout", Origin, csrf);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value => value.StartsWith(BrowserSession.CookieName + "="));
        var signedOut = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.Equal(JsonValueKind.Null, signedOut.GetProperty("user").ValueKind);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Theory]
    [InlineData(null, "start-session")]
    [InlineData("https://another.example.test", "start-session")]
    [InlineData(Origin, null)]
    [InlineData(Origin, "unrecognized")]
    public async Task BrowserRegistrationRequiresTheTrustedOriginAndRequestHeader(string? origin, string? header)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var response = await Start(client, true, origin, header);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(2, await app.ReadDb(db => db.Organizations.CountAsync()));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("prior-session")]
    [InlineData("foreign-origin")]
    [InlineData("missing-origin")]
    public async Task CookieWritesRequireTheCurrentSessionAndTheOriginThatDisplayedTheForm(string reason)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        (await Start(client, false)).EnsureSuccessStatusCode();
        var oldState = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        (await Start(client, false)).EnsureSuccessStatusCode();
        var state = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        var token = state.GetProperty("csrfToken").GetString();
        var previous = oldState.GetProperty("csrfToken").GetString();
        Assert.NotEqual(previous, token);
        var origin = reason switch { "foreign-origin" => "https://another.example.test", "missing-origin" => null, _ => Origin };
        var requestToken = reason switch { "missing" => null, "prior-session" => previous, _ => token };
        var response = await Write(client, HttpMethod.Delete, "/api/settings/users/2", origin, requestToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await app.ReadDb(db => db.Users.AnyAsync(u => u.Id == 2)));
        Assert.Empty(await app.ReadDb(db => db.AuditLogs.ToListAsync()));
        Assert.Equal(HttpStatusCode.OK, (await Write(client, HttpMethod.Delete, "/api/settings/users/2", Origin, token)).StatusCode);
    }

    [Fact]
    public async Task ARevokedCookieIsTreatedAsSignedOutOnTheNextPageLoad()
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        (await Start(client, false)).EnsureSuccessStatusCode();
        await app.ReadDb(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == 1);
            db.Users.Remove(user);
            return await db.SaveChangesAsync();
        });
        var session = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.Equal(JsonValueKind.Null, session.GetProperty("user").ValueKind);
        Assert.Equal(JsonValueKind.Null, session.GetProperty("csrfToken").ValueKind);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Fact]
    public async Task CredentialsAreRequiredForBothTransportsAndTheyShareOneLoginLimit()
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var empty = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.Equal(JsonValueKind.Null, empty.GetProperty("user").ValueKind);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var path = attempt % 2 == 0 ? "/api/auth/login" : "/api/auth/session/login";
            var denied = await Write(client, HttpMethod.Post, path, Origin, "start-session",
                new { email = "user1@example.test", password = "incorrect" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.False(denied.Headers.Contains("Set-Cookie"));
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Start(client, false)).StatusCode);
    }

    private static Task<HttpResponseMessage> Start(HttpClient client, bool register, string? origin = Origin, string? header = "start-session")
        => register
            ? Write(client, HttpMethod.Post, "/api/auth/session/register", origin, header,
                new { organizationName = "Browser team", adminFullName = "Browser owner", email = "new@example.test", password = SettingsApp.Password })
            : Write(client, HttpMethod.Post, "/api/auth/session/login", origin, header,
                new { email = "user1@example.test", password = SettingsApp.Password });

    private static async Task<HttpResponseMessage> Write(HttpClient client, HttpMethod method, string path, string? origin, string? csrf, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (origin is not null) request.Headers.Add("Origin", origin);
        if (csrf is not null) request.Headers.Add(CsrfHeader, csrf);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}
