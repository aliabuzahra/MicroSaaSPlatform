using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.HasIndex(u => new { u.Email, u.TenantId }).IsUnique();
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            builder.Ignore(u => u.DomainEvents);

            builder.Property(u => u.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Token).IsRequired().HasMaxLength(256);
            builder.HasIndex(r => r.Token).IsUnique();
            builder.HasIndex(r => r.UserId);
            builder.Ignore(r => r.DomainEvents);
        });
    }
}
