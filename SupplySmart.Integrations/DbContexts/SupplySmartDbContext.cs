using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SupplySmart.Integrations.Models.Data;

namespace SupplySmart.Integrations.DbContexts
{
    public sealed class SupplySmartDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Location> Locations => Set<Location>();

        public SupplySmartDbContext(DbContextOptions<SupplySmartDbContext> options) : base(options)
        {
            _httpContextAccessor = new HttpContextAccessor();
            ChangeTracker.StateChanged += UpdateTimestamps;
            ChangeTracker.Tracked += UpdateTimestamps;
        }

        public SupplySmartDbContext(DbContextOptions<SupplySmartDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            ChangeTracker.StateChanged += UpdateTimestamps;
            ChangeTracker.Tracked += UpdateTimestamps;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.ToTable("Organizations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.HasIndex(e => e.Email)
                      .IsUnique();

                entity.Property(e => e.Role)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .IsRequired();

                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Users)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

                entity.HasOne(e => e.Organization)
                      .WithMany(o => o.Locations)
                      .HasForeignKey(e => e.OrganizationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
        {
            if (e.Entry.Entity is not IEntityBase entityWithTimeStamps) return;

            var now = DateTime.UtcNow;
            var userName = GetFormattedUserName(_httpContextAccessor.HttpContext?.User?.Claims);

            switch (e.Entry.State)
            {
                case EntityState.Modified:
                    entityWithTimeStamps.LastUpdatedDateUtc = now;
                    entityWithTimeStamps.LastUpdatedBy = userName;
                    break;
                case EntityState.Added:
                    entityWithTimeStamps.CreatedDateUtc = now;
                    entityWithTimeStamps.LastUpdatedDateUtc = now;
                    if (string.IsNullOrWhiteSpace(entityWithTimeStamps.CreatedBy))
                        entityWithTimeStamps.CreatedBy = userName;
                    if (string.IsNullOrWhiteSpace(entityWithTimeStamps.LastUpdatedBy))
                        entityWithTimeStamps.LastUpdatedBy = userName;
                    break;
            }
        }

        private static string GetFormattedUserName(IEnumerable<Claim>? claims)
        {
            if (claims == null) return "System/Unauthenticated";
            var claimsList = claims.ToList();
            var email = claimsList.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value ?? "n/a";
            return $"User ({email})";
        }
    }
}