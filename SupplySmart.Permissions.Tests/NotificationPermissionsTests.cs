using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace SupplySmart.Tests;

public class NotificationPermissionsTests
{
    [Theory]
    [InlineData(0, HttpStatusCode.Unauthorized)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(3, HttpStatusCode.Forbidden)]
    [InlineData(4, HttpStatusCode.Forbidden)]
    [InlineData(5, HttpStatusCode.OK)]
    public async Task OnlyAdminsCanDispatchTestMailForTheirOrganization(int userId, HttpStatusCode expected)
    {
        using var app = new SettingsApp();
        var client = userId == 0 ? app.NewClient() : await app.SignIn(userId);
        var response = await client.PostAsJsonAsync("/api/settings/notifications/test?organizationId=2", new
        {
            triggerType = "Item Reaches Low-Stock Limit",
            templateMarkdown = "Test inventory alert",
            additionalRecipients = new[] { "external@example.test" }
        });
        Assert.Equal(expected, response.StatusCode);

        if (expected != HttpStatusCode.OK)
        {
            Assert.Empty(app.Email.Recipients);
            Assert.Equal(0, await app.ReadDb(db => db.NotificationTemplates.CountAsync()));
        }
        else
        {
            var recipients = userId == 1
                ? new[] { "external@example.test", "user1@example.test", "user2@example.test", "user4@example.test" }
                : new[] { "external@example.test", "user5@example.test" };
            Assert.Equal(recipients, app.Email.Recipients.Order().ToArray());
        }
    }
}
