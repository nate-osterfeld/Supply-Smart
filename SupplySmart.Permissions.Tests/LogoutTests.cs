using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace SupplySmart.Tests;

public class LogoutTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task SignOutInvalidatesTheAccountsSessionsAndPreservesOtherAccounts(int userId)
    {
        using var app = new SettingsApp();
        using var original = await app.SignIn(userId);
        using var secondSession = await app.SignIn(userId);
        using var otherAccount = await app.SignIn(2);
        var response = await original.PostAsJsonAsync("/api/auth/logout?userId=2", new { userId = 2 });
        var oldAccess = await original.GetAsync("/api/settings/users/");
        output.WriteLine($"Sign-out response: {(int)response.StatusCode}; previous session access: {(int)oldAccess.StatusCode}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldAccess.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await secondSession.GetAsync("/api/settings/users/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otherAccount.GetAsync("/api/settings/users/")).StatusCode);
        using var fresh = await app.SignIn(userId);
        Assert.Equal(HttpStatusCode.OK, (await fresh.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Fact]
    public async Task AnonymousSignOutCannotInvalidateAnotherAccount()
    {
        using var app = new SettingsApp();
        using var account = await app.SignIn(1);
        using var anonymous = app.NewClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/logout", new { userId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await account.GetAsync("/api/settings/users/")).StatusCode);
    }
}
