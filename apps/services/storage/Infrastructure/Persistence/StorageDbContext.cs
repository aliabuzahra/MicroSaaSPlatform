using Microsoft.EntityFrameworkCore;
using SaaS.Storage.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Storage.Service.Infrastructure.Persistence;

public class StorageDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<StoredFile> Files { get; set; }

    public StorageDbContext(DbContextOptions<StorageDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.FileName).IsRequired().HasMaxLength(255);
            builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(f => f.StoragePath).IsRequired().HasMaxLength(500);
            builder.Property(f => f.Category).HasMaxLength(50);
            builder.Property(f => f.Description).HasMaxLength(500);
            
            builder.Property(f => f.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasIndex(f => f.TenantId);
            builder.HasIndex(f => f.StoragePath).IsUnique();
            builder.HasIndex(f => new { f.TenantId, f.Category });
            
            builder.HasQueryFilter(f => f.TenantId == _tenantContext.TenantId && f.DeletedAt == null);
            builder.Ignore(f => f.DomainEvents);
        });
    }
}
