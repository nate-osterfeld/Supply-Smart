using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class NotificationPermissionsTests
{
    [Theory]
    [InlineData(0, HttpStatusCode.Unauthorized)]
    [InlineData(3, HttpStatusCode.Forbidden)]
    [InlineData(4, HttpStatusCode.Forbidden)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(5, HttpStatusCode.OK)]
    public async Task OnlyClinicAdminsCanReadNotificationRouting(int userId, HttpStatusCode expected)
    {
        using var app = new SettingsApp();
        using var client = userId == 0 ? app.NewClient() : await app.SignIn(userId);
        await app.ReadDb(async db =>
        {
            foreach (int clinic in new[] { 1, 2 })
                db.NotificationTemplates.Add(new NotificationTemplate
                {
                    OrganizationId = clinic, TriggerType = NotificationService.TriggerLowStock,
                    TemplateMarkdown = "Low stock", AdditionalRecipients = $"purchasing-{clinic}@example.test"
                });
            return await db.SaveChangesAsync();
        });

        using var response = await client.GetAsync("/api/settings/notifications");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var templates = await response.Content.ReadFromJsonAsync<List<NotificationTemplateDto>>();
            var recipients = templates!.SelectMany(t => t.AdditionalRecipients);
            Assert.Equal(userId == 5 ? "purchasing-2@example.test" : "purchasing-1@example.test", Assert.Single(recipients));
        }
        else
        {
            Assert.DoesNotContain("purchasing-", await response.Content.ReadAsStringAsync());
            Assert.Equal(2, await app.ReadDb(db => db.NotificationTemplates.CountAsync()));
        }
    }

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
