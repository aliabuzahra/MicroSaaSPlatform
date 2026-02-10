using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;

namespace SaaS.Tenant.Service.Infrastructure.Persistence;

public class TenantDbContext : DbContext
{
    public DbSet<Domain.Entities.Tenant> Tenants { get; set; }

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
    }
}
