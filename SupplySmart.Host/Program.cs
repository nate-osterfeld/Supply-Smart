using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupplySmart.Domain.Services;
using SupplySmart.Endpoints.Configuration;
using SupplySmart.Integrations.DbContexts;
using SupplySmart.Integrations.Repositories;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Load secrets file
builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

// Register Entity Framework Core with SQL Server using your custom DbContext name
builder.Services.AddDbContext<SupplySmartDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("supplysmart-db")));

// Register repositories & services for dependency injection
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(SupplySmartRepository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Wait a minute before sending another test email." }, cancellationToken);
    options.AddPolicy("email-template-tests", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 2, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<ITenantSeederService, TenantSeederService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddHttpContextAccessor(); // Required for automatic audit logging timestamps

// Configure JWT settings
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal!;
            // Tokens issued before this migration have version zero.
            var versionClaim = principal.FindFirstValue("session_version") ?? "0";
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
                !int.TryParse(principal.FindFirstValue("OrganizationId"), out var orgId) ||
                !int.TryParse(versionClaim, out var version))
            {
                context.Fail("Invalid session.");
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<SupplySmartDbContext>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);
            if (user is null || user.OrganizationId != orgId || user.SessionVersion != version ||
                user.Role.ToString() != principal.FindFirstValue(ClaimTypes.Role))
                context.Fail("This session is no longer active. Sign in again.");
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Enable serving static files from wwwroot (React frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// Map API endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapEmailTemplateEndpoints();
app.MapDashboardEndpoints();
app.MapLocationEndpoints();
app.MapCatalogEndpoints();

// Fallback routing for React Single Page Application (SPA) client-side routing
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
