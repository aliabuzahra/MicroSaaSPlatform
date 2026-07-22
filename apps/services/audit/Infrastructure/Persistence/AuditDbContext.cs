using Microsoft.EntityFrameworkCore;
using SaaS.Audit.Service.Domain.Entities;

namespace SaaS.Audit.Service.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; }

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).HasMaxLength(100);
            builder.Property(a => a.UserEmail).HasMaxLength(255);
            builder.Property(a => a.IpAddress).HasMaxLength(50);
            builder.Property(a => a.Details).HasColumnType("jsonb");
            
            builder.HasIndex(a => a.TenantId);
            builder.HasIndex(a => a.Timestamp);
            builder.HasIndex(a => new { a.TenantId, a.EntityType });
            builder.HasIndex(a => new { a.TenantId, a.Action });
            
            builder.Ignore(a => a.DomainEvents);
        });
    }
}
