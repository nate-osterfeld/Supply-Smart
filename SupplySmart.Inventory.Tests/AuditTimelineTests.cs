using System.Net;
using System.Net.Http.Json;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Tests;

public class AuditTimelineTests
{
    [Theory]
    [InlineData("/api/catalog/audit-logs")]
    [InlineData("/api/audit-ledger/recent")]
    public async Task AuditTimestampsIdentifyTheUtcInstantAfterReadingFromStorage(string route)
    {
        using var app = new SettingsApp();
        using var reader = await app.SignIn(1);
        var instant = new DateTime(2026, 3, 8, 6, 59, 59, DateTimeKind.Utc).AddMilliseconds(123);
        await app.ReadDb(async db =>
        {
            db.AuditLogs.Add(new AuditLog { OrganizationId = 1, Timestamp = instant });
            return await db.SaveChangesAsync();
        });

        var entries = await reader.GetFromJsonAsync<AuditLogEntryDto[]>(route);
        var timestamp = Assert.Single(entries!).Timestamp;
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.Equal(instant, timestamp);
    }

    [Theory]
    [InlineData("/api/catalog/audit-logs", 50)]
    [InlineData("/api/audit-ledger/recent", 5)]
    public async Task EachFeedReturnsTheNewestEntriesForTheSignedInClinic(string route, int limit)
    {
        using var app = new SettingsApp();
        using var guest = app.NewClient();
        using var first = await app.SignIn(1);
        using var second = await app.SignIn(5);
        var date = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        await app.ReadDb(async db =>
        {
            for (var id = 1; id <= 60; id++)
                db.AuditLogs.Add(new AuditLog { Id = id, OrganizationId = 1, Timestamp = date });
            db.AuditLogs.Add(new AuditLog { Id = 61, OrganizationId = 1, Timestamp = date.AddDays(-1) });
            db.AuditLogs.Add(new AuditLog { Id = 62, OrganizationId = 2, Timestamp = date.AddDays(1) });
            return await db.SaveChangesAsync();
        });

        using var unauthorized = await guest.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        var history = await first.GetFromJsonAsync<AuditLogEntryDto[]>(route + "?organizationId=2");
        Assert.Equal(Enumerable.Range(61 - limit, limit).Reverse(), history!.Select(entry => entry.Id));
        var other = await second.GetFromJsonAsync<AuditLogEntryDto[]>(route + "?organizationId=1");
        Assert.Equal(62, Assert.Single(other!).Id);
    }
}
