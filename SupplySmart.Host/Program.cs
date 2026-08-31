using SupplySmart.Integrations.DbContexts;
using SupplySmart.Endpoints.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Load secrets file
builder.Configuration.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

// Register Entity Framework Core with SQL Server using your custom DbContext name
builder.Services.AddDbContext<SupplySmartDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("supplysmart-db")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable serving static files from wwwroot (React frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// Map modular inventory endpoints
app.MapSupplyEndpoints();

// Fallback routing for React Single Page Application (SPA) client-side routing
app.MapFallbackToFile("index.html");

app.Run();