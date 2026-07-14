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
            builder.Property(a => a.EventType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
            builder.Property(a => a.UserEmail).HasMaxLength(255);
            builder.Property(a => a.ResourceType).HasMaxLength(100);
            builder.Property(a => a.ResourceId).HasMaxLength(255);
            builder.Property(a => a.IpAddress).HasMaxLength(50);
            builder.Property(a => a.UserAgent).HasMaxLength(500);
            builder.Property(a => a.CorrelationId).HasMaxLength(100);
            builder.Property(a => a.Severity).HasConversion<string>().HasMaxLength(20);
            
            builder.HasIndex(a => a.Timestamp);
            builder.HasIndex(a => a.EventType);
            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => a.TenantId);
            builder.HasIndex(a => new { a.ResourceType, a.ResourceId });
            builder.HasIndex(a => a.CorrelationId);
            
            builder.Ignore(a => a.DomainEvents);
        });
    }
}
