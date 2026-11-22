using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public class ClinicSetupAuditTests
{
    [Fact]
    public async Task RegistrationRecordsTheStarterInventoryThatWasActuallyCreated()
    {
        const string email = "founder@example.test";
        using var app = new SettingsApp();
        using var client = app.NewClient();
        var start = DateTime.UtcNow;
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            OrganizationName = "New clinic", AdminFullName = "Clinic founder",
            Email = email, Password = "Local registration passphrase"
        });
        registration.EnsureSuccessStatusCode();
        var identity = (await registration.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identity.Token);
        var itemCount = await app.ReadDb(db => db.InventoryItems.CountAsync(i => i.OrganizationId == identity.OrganizationId));
        Assert.True(itemCount > 0);
        foreach (var route in new[] { "/api/catalog/audit-logs", "/api/audit-ledger/recent" })
        {
            var history = await client.GetFromJsonAsync<AuditLogEntryDto[]>(route);
            var entry = Assert.Single(history!);
            Assert.Equal("Workspace initialized", entry.ActionType);
            Assert.Equal(email, entry.UserEmail);
            Assert.Equal($"Created {itemCount} starter inventory items.", entry.ItemDetails);
            Assert.InRange(entry.Timestamp, start, DateTime.UtcNow);
        }
    }
}
