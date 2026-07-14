using Microsoft.EntityFrameworkCore;
using SaaS.ApiKeys.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.ApiKeys.Service.Infrastructure.Persistence;

public class ApiKeysDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<ApiKey> ApiKeys { get; set; }

    public ApiKeysDbContext(DbContextOptions<ApiKeysDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(builder =>
        {
            builder.HasKey(k => k.Id);
            builder.Property(k => k.Name).IsRequired().HasMaxLength(100);
            builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(256);
            builder.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(20);
            builder.Property(k => k.Scopes).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            builder.Property(k => k.RevokedReason).HasMaxLength(500);
            
            builder.Property(k => k.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasIndex(k => k.KeyPrefix);
            builder.HasIndex(k => k.TenantId);
            builder.HasIndex(k => k.KeyHash).IsUnique();
            
            builder.HasQueryFilter(k => k.TenantId == _tenantContext.TenantId);
            builder.Ignore(k => k.DomainEvents);
        });
    }
}
