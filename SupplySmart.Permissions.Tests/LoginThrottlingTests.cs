using System.Net;
using System.Net.Http.Json;

namespace SupplySmart.Tests;

public class LoginThrottlingTests
{
    [Theory]
    [InlineData("user1@example.test")]
    [InlineData("missing@example.test")]
    public async Task RepeatedGuessesAreLimitedEvenWhenTheClientChangesHeaders(string email)
    {
        using var app = new SettingsApp();
        using var client = app.NewClient();
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var nextClient = app.NewClient();
        nextClient.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2.123");
        var blocked = await nextClient.PostAsJsonAsync("/api/auth/login", new { email = email.ToUpperInvariant(), password = SettingsApp.Password });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.NotNull(blocked.Headers.RetryAfter);

        var otherAccount = await nextClient.PostAsJsonAsync("/api/auth/login", new { email = "user3@example.test", password = SettingsApp.Password });
        Assert.Equal(HttpStatusCode.OK, otherAccount.StatusCode);
    }
}
