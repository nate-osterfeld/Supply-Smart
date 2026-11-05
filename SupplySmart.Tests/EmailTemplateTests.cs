using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class EmailTemplateTests
{
    private const string Url = "/api/settings/email-templates";
    private static object Draft(string subject = "Low stock: {{item_name}}", string body = "Remaining: {{current_quantity}} at {{supply_room}}.", int revision = 0) => new { subject, body, revision };

    [Fact]
    public async Task TemplatesAreAdminOnly()
    {
        using var app = new SettingsApp();
        using var anonymous = app.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Url)).StatusCode);
        foreach (var id in new[] { 3, 4 })
        {
            using var member = await app.SignIn(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync(Url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync(Url + "/low-stock", Draft())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync(Url + "/low-stock/test", Draft())).StatusCode);
        }
        Assert.Empty(app.Email.Alerts);
    }

    [Fact]
    public async Task TemplatesPersistPerOrganizationAndPerTrigger()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        var original = await admin.GetFromJsonAsync<EmailTemplateDto[]>(Url);
        Assert.Equal(3, original!.Length);
        Assert.All(original, template => Assert.Equal(0, template.Revision));
        var response = await admin.PutAsJsonAsync(Url + "/low-stock", Draft("Please check {{item_name}}"));
        response.EnsureSuccessStatusCode();
        var saved = await response.Content.ReadFromJsonAsync<EmailTemplateDto>();
        Assert.Equal(1, saved!.Revision);
        Assert.Equal("user1@example.test", saved.UpdatedBy);
        Assert.NotNull(saved.UpdatedAtUtc);
        // A fresh HTTP session retrieves the saved template, not local component state.
        using var fresh = await app.SignIn(2);
        var reloaded = await fresh.GetFromJsonAsync<EmailTemplateDto[]>(Url);
        Assert.Equal(saved.Subject, reloaded!.Single(t => t.Trigger == "low-stock").Subject);
        Assert.Equal(0, reloaded!.Single(t => t.Trigger == "out-of-stock").Revision);
        (await admin.PutAsJsonAsync(Url + "/out-of-stock", Draft("Empty: {{item_name}}"))).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync(Url + "/weekly-digest", Draft("{{organization_name}} weekly update", "{{inventory_summary}} as of {{week_ending}}"))).EnsureSuccessStatusCode();
        using var otherTeam = await app.SignIn(5);
        Assert.All((await otherTeam.GetFromJsonAsync<EmailTemplateDto[]>(Url))!, t => Assert.Equal(0, t.Revision));
        Assert.Equal(3, await app.ReadDb(db => db.EmailTemplates.CountAsync()));
        Assert.Equal(3, await app.ReadDb(db => db.AuditLogs.CountAsync(a => a.ActionType == "Email Template Updated")));
    }

    [Fact]
    public async Task AStaleSaveCannotOverwriteSomeoneElsesEdit()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        (await admin.PutAsJsonAsync(Url + "/low-stock", Draft("First change"))).EnsureSuccessStatusCode();
        var stale = await admin.PutAsJsonAsync(Url + "/low-stock", Draft("Old tab"));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        (await admin.PutAsJsonAsync(Url + "/low-stock", Draft("Second change", revision: 1))).EnsureSuccessStatusCode();
        Assert.Equal("Second change", (await admin.GetFromJsonAsync<EmailTemplateDto[]>(Url))!.Single(t => t.Trigger == "low-stock").Subject);
    }

    [Theory]
    [InlineData("", "Message")]
    [InlineData("Subject\r\nBcc: someone@example.test", "Message")]
    [InlineData("Subject", "")]
    [InlineData("{{unknown}}", "Message")]
    [InlineData("Subject", "{{ item_name }")]
    [InlineData("Subject", "{{inventory_summary}}")]
    public async Task InvalidDraftsAreRejectedForSavingAndSending(string subject, string body)
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        var draft = Draft(subject, body);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Url + "/low-stock", draft)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Url + "/low-stock/test", draft)).StatusCode);
        Assert.Equal(0, await app.ReadDb(db => db.EmailTemplates.CountAsync()));
        Assert.Empty(app.Email.Alerts);
    }

    [Fact]
    public async Task OversizedTemplatesAndUnsupportedTriggersAreRejected()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Url + "/low-stock", Draft(new string('a', 201)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Url + "/low-stock", Draft(body: new string('a', 8001)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Url + "/unknown", Draft())).StatusCode);
    }

    [Fact]
    public async Task TestUsesDraftAndSampleValuesOnlySendsToCurrentAdminAndDoesNotSave()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        var response = await admin.PostAsJsonAsync(Url + "/low-stock/test", new
        {
            subject = "Please check {{ item_name }}",
            body = "{{organization_name}}: {{current_quantity}} left at {{supply_room}}. Minimum {{minimum_threshold}}.",
            toEmail = "someone-else@example.test", organizationId = 2
        });
        response.EnsureSuccessStatusCode();
        var sent = Assert.Single(app.Email.Alerts);
        Assert.Equal("user1@example.test", sent.Email);
        Assert.Equal("[TEST] Please check Sample gloves", sent.Subject);
        Assert.Contains("First team: 4 left at Sample supply room. Minimum 10.", sent.Body);
        Assert.DoesNotContain("{{", sent.Body);
        Assert.Equal(0, await app.ReadDb(db => db.EmailTemplates.CountAsync()));
    }

    [Fact]
    public async Task DeliveryFailureDoesNotReportSuccess()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        app.Email.Fail = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await admin.PostAsJsonAsync(Url + "/low-stock/test", Draft())).StatusCode);
        Assert.Empty(app.Email.Alerts);
    }

    [Fact]
    public async Task TestEmailsAreLimitedPerUser()
    {
        using var app = new SettingsApp();
        using var admin = await app.SignIn(1);
        for (var i = 0; i < 2; i++) (await admin.PostAsJsonAsync(Url + "/low-stock/test", Draft())).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.TooManyRequests, (await admin.PostAsJsonAsync(Url + "/low-stock/test", Draft())).StatusCode);
        Assert.Equal(2, app.Email.Alerts.Count);
        using var secondAdmin = await app.SignIn(2);
        (await secondAdmin.PostAsJsonAsync(Url + "/low-stock/test", Draft())).EnsureSuccessStatusCode();
        Assert.Equal("user2@example.test", app.Email.Alerts.Last().Email);
    }

    [Fact]
    public void RenderingDoesNotInterpretVariablesInsideInventoryValues()
    {
        var result = EmailTemplateService.Render("Item: {{item_name}}", new Dictionary<string, string> { ["item_name"] = "{{organization_name}} <sample>" });
        Assert.Equal("Item: {{organization_name}} <sample>", result);
    }
}
