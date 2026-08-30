using Microsoft.EntityFrameworkCore;
using SupplySmart.Integrations.Models;

namespace SupplySmart.Integrations.DbContexts;

public class SupplySmartDbContext : DbContext
{
    public SupplySmartDbContext(DbContextOptions<SupplySmartDbContext> options) : base(options) { }

    public DbSet<SupplyItem> SupplyItems => Set<SupplyItem>();
}