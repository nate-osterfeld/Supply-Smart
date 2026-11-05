using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupplySmart.Domain.Services;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;

namespace SupplySmart.Tests;

public sealed class SettingsApp : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public FakeEmail Email { get; } = new();
    public const string Password = "Local-test-password-423!";
    private bool seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("JwtSettings:Secret", "local-tests-only-signing-key-at-least-sixty-four-characters-123456789");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<SupplySmartDbContext>();
            services.RemoveAll<DbContextOptions<SupplySmartDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SupplySmartDbContext>>();
            services.AddDbContext<SupplySmartDbContext>(options => options.UseSqlite(connection));
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
        });
    }

    public HttpClient NewClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (!seeded)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SupplySmartDbContext>();
            db.Database.EnsureCreated();
            db.Organizations.AddRange(new Organization { Id = 1, Name = "First team" }, new Organization { Id = 2, Name = "Other team" });
            AddUser(db, 1, 1, UserRole.Admin);
            AddUser(db, 2, 1, UserRole.Admin);
            AddUser(db, 3, 1, UserRole.Staff);
            AddUser(db, 4, 1, UserRole.Manager);
            AddUser(db, 5, 2, UserRole.Admin);
            db.SaveChanges();
            seeded = true;
        }
        return client;
    }

    public async Task<HttpClient> SignIn(int userId)
    {
        var client = NewClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = $"user{userId}@example.test", password = Password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    public async Task<T> ReadDb<T>(Func<SupplySmartDbContext, Task<T>> read)
    {
        using var scope = Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<SupplySmartDbContext>());
    }

    private static void AddUser(SupplySmartDbContext db, int id, int org, UserRole role)
    {
        var user = new User { Id = id, OrganizationId = org, FullName = $"User {id}", Email = $"user{id}@example.test", Role = role };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Password);
        db.Users.Add(user);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

public sealed class FakeEmail : IEmailService
{
    public bool Fail { get; set; }
    public List<(string Email, string Password)> Invitations { get; } = [];
    public List<(string Email, string Subject, string Body)> Alerts { get; } = [];
    public Task SendAlertEmailAsync(string toEmail, string subject, string body)
    {
        if (Fail) throw new SmtpException("Simulated delivery failure.");
        Alerts.Add((toEmail, subject, body));
        return Task.CompletedTask;
    }
    public Task SendInviteEmailAsync(string toEmail, string fullName, string tempPassword)
    {
        if (Fail) throw new SmtpException("Simulated delivery failure.");
        Invitations.Add((toEmail, tempPassword));
        return Task.CompletedTask;
    }
}
