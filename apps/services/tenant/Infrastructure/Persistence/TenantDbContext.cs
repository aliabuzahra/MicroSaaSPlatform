using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;

namespace SaaS.Tenant.Service.Infrastructure.Persistence;

public class TenantDbContext : DbContext
{
    public DbSet<Domain.Entities.Tenant> Tenants { get; set; }
    public DbSet<TenantSettings> Settings { get; set; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);
        
        modelBuilder.Entity<Domain.Entities.Tenant>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Slug).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Slug).IsUnique();
            builder.Property(t => t.SubscriptionPlan).IsRequired();
            builder.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<TenantSettings>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Value).IsRequired();
            builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
            builder.Ignore(s => s.DomainEvents);
        });
    }
}
