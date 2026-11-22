using System.Net;
using System.Net.Http.Json;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Tests;

public class AuditReadPermissionsTests
{
    [Theory]
    [InlineData(0, HttpStatusCode.Unauthorized)]
    [InlineData(3, HttpStatusCode.Forbidden)]
    [InlineData(4, HttpStatusCode.OK)]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(5, HttpStatusCode.OK)]
    public async Task BothAuditFeedsRequireAdminOrManagerInTheReadersClinic(int userId, HttpStatusCode expected)
    {
        using var app = new SettingsApp();
        using var client = userId == 0 ? app.NewClient() : await app.SignIn(userId);
        await app.ReadDb(async db =>
        {
            foreach (var clinic in new[] { 1, 2 })
                db.AuditLogs.Add(new AuditLog { OrganizationId = clinic, UserEmail = $"admin{clinic}@example.test", ActionType = "User Invited" });
            return await db.SaveChangesAsync();
        });
        foreach (var route in new[] { "/api/catalog/audit-logs", "/api/audit-ledger/recent" })
        {
            using var result = await client.GetAsync(route + "?organizationId=2");
            Assert.Equal(expected, result.StatusCode);
            if (expected == HttpStatusCode.OK)
            {
                var rows = await result.Content.ReadFromJsonAsync<AuditLogEntryDto[]>();
                Assert.Equal(userId == 5 ? "admin2@example.test" : "admin1@example.test", Assert.Single(rows!).UserEmail);
            }
            else
                Assert.DoesNotContain("@example.test", await result.Content.ReadAsStringAsync());
        }
        if (userId == 3)
        {
            using var catalog = await client.GetAsync("/api/catalog");
            using var dashboard = await client.GetAsync("/api/dashboard/metrics");
            Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
            Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        }
    }
}
