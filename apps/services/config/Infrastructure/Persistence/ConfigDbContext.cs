using Microsoft.EntityFrameworkCore;
using SaaS.Config.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Config.Service.Infrastructure.Persistence;

public class ConfigDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<TenantSettings> TenantSettings { get; set; }
    public DbSet<GlobalSettings> GlobalSettings { get; set; }

    public ConfigDbContext(DbContextOptions<ConfigDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSettings>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Value).IsRequired();
            builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
            
            builder.Property(s => s.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
            builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            builder.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<GlobalSettings>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Value).IsRequired();
            builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(s => s.Description).HasMaxLength(500);
            builder.HasIndex(s => s.Key).IsUnique();
            builder.Ignore(s => s.DomainEvents);
        });

        SeedDefaults(modelBuilder);
    }

    private static void SeedDefaults(ModelBuilder modelBuilder)
    {
        var globalSettings = new List<GlobalSettings>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Key = "platform_name", Value = "SaaS Platform", Type = SettingType.String, Description = "Platform display name", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Key = "default_timezone", Value = "UTC", Type = SettingType.String, Description = "Default timezone for new tenants", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Key = "default_locale", Value = "en-US", Type = SettingType.String, Description = "Default locale for new tenants", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Key = "maintenance_mode", Value = "false", Type = SettingType.Boolean, Description = "Enable maintenance mode", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Key = "signup_enabled", Value = "true", Type = SettingType.Boolean, Description = "Allow new signups", CreatedAt = DateTime.UtcNow },
        };

        modelBuilder.Entity<GlobalSettings>().HasData(globalSettings);
    }
}
