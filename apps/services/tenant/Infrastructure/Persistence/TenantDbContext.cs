using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;

namespace SaaS.Tenant.Service.Infrastructure.Persistence;

public class TenantDbContext : DbContext
{
    public DbSet<Domain.Entities.Tenant> Tenants { get; set; }
    public DbSet<TenantMember> TenantMembers { get; set; }
    public DbSet<TenantInvitation> TenantInvitations { get; set; }

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
            builder.Property(t => t.SubscriptionPlan).IsRequired().HasMaxLength(50);
            builder.Property(t => t.ContactEmail).HasMaxLength(255);
            builder.Property(t => t.LogoUrl).HasMaxLength(500);
            builder.Property(t => t.Description).HasMaxLength(1000);
            builder.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<TenantMember>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.TenantId).IsRequired();
            builder.Property(m => m.UserId).IsRequired();
            builder.Property(m => m.Email).IsRequired().HasMaxLength(255);
            builder.Property(m => m.Role).IsRequired().HasMaxLength(50);
            builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();
            builder.HasIndex(m => m.TenantId);
            builder.Ignore(m => m.DomainEvents);
        });

        modelBuilder.Entity<TenantInvitation>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.TenantId).IsRequired();
            builder.Property(i => i.Email).IsRequired().HasMaxLength(255);
            builder.Property(i => i.Token).IsRequired().HasMaxLength(256);
            builder.Property(i => i.Role).IsRequired().HasMaxLength(50);
            builder.HasIndex(i => i.Token).IsUnique();
            builder.HasIndex(i => new { i.TenantId, i.Email });
            builder.Ignore(i => i.DomainEvents);
        });
    }
}
