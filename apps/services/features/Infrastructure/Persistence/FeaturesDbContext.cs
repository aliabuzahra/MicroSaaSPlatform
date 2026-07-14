using Microsoft.EntityFrameworkCore;
using SaaS.Features.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Features.Service.Infrastructure.Persistence;

public class FeaturesDbContext : DbContext
{
    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<PlanFeature> PlanFeatures { get; set; }
    public DbSet<TenantFeatureOverride> TenantOverrides { get; set; }

    public FeaturesDbContext(DbContextOptions<FeaturesDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeatureFlag>(builder =>
        {
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Key).IsRequired().HasMaxLength(100);
            builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
            builder.Property(f => f.Description).HasMaxLength(500);
            builder.Property(f => f.Type).HasConversion<string>().HasMaxLength(20);
            builder.Property(f => f.DefaultValue).HasMaxLength(1000);
            builder.HasIndex(f => f.Key).IsUnique();
            builder.Ignore(f => f.DomainEvents);
        });

        modelBuilder.Entity<PlanFeature>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PlanId).IsRequired().HasMaxLength(100);
            builder.Property(p => p.FeatureKey).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Value).HasMaxLength(1000);
            builder.HasIndex(p => new { p.PlanId, p.FeatureKey }).IsUnique();
            builder.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<TenantFeatureOverride>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.FeatureKey).IsRequired().HasMaxLength(100);
            builder.Property(o => o.Value).HasMaxLength(1000);
            builder.Property(o => o.Reason).HasMaxLength(500);
            
            builder.Property(o => o.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasIndex(o => new { o.TenantId, o.FeatureKey }).IsUnique();
            builder.Ignore(o => o.DomainEvents);
        });

        SeedDefaultFeatures(modelBuilder);
    }

    private static void SeedDefaultFeatures(ModelBuilder modelBuilder)
    {
        var features = new List<FeatureFlag>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Key = StandardFeatures.ApiAccess, Name = "API Access", Description = "Access to REST API", IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Key = StandardFeatures.WebhooksEnabled, Name = "Webhooks", Description = "Outbound webhook subscriptions", IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Key = StandardFeatures.CustomDomain, Name = "Custom Domain", Description = "Use your own domain", IsEnabled = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Key = StandardFeatures.SsoEnabled, Name = "Single Sign-On", Description = "SAML/OIDC SSO", IsEnabled = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Key = StandardFeatures.AuditLogs, Name = "Audit Logs", Description = "View audit logs", IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Key = StandardFeatures.AdvancedAnalytics, Name = "Advanced Analytics", Description = "Advanced analytics dashboard", IsEnabled = false, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Key = StandardFeatures.PrioritySupport, Name = "Priority Support", Description = "Priority support queue", IsEnabled = false, CreatedAt = DateTime.UtcNow },
        };
        modelBuilder.Entity<FeatureFlag>().HasData(features);

        var planFeatures = new List<PlanFeature>
        {
            // Free Plan
            new() { Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"), PlanId = "free", FeatureKey = StandardFeatures.ApiAccess, IsEnabled = true, Limit = 1000, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"), PlanId = "free", FeatureKey = StandardFeatures.MaxUsers, IsEnabled = true, Limit = 3, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"), PlanId = "free", FeatureKey = StandardFeatures.MaxApiKeys, IsEnabled = true, Limit = 2, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"), PlanId = "free", FeatureKey = StandardFeatures.MaxWebhooks, IsEnabled = false, Limit = 0, CreatedAt = DateTime.UtcNow },
            
            // Pro Plan
            new() { Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"), PlanId = "pro", FeatureKey = StandardFeatures.ApiAccess, IsEnabled = true, Limit = 50000, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"), PlanId = "pro", FeatureKey = StandardFeatures.WebhooksEnabled, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("b3333333-3333-3333-3333-333333333333"), PlanId = "pro", FeatureKey = StandardFeatures.MaxUsers, IsEnabled = true, Limit = 20, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("b4444444-4444-4444-4444-444444444444"), PlanId = "pro", FeatureKey = StandardFeatures.MaxApiKeys, IsEnabled = true, Limit = 10, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("b5555555-5555-5555-5555-555555555555"), PlanId = "pro", FeatureKey = StandardFeatures.MaxWebhooks, IsEnabled = true, Limit = 5, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("b6666666-6666-6666-6666-666666666666"), PlanId = "pro", FeatureKey = StandardFeatures.AuditLogs, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            
            // Enterprise Plan
            new() { Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"), PlanId = "enterprise", FeatureKey = StandardFeatures.ApiAccess, IsEnabled = true, Limit = -1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c2222222-2222-2222-2222-222222222222"), PlanId = "enterprise", FeatureKey = StandardFeatures.WebhooksEnabled, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"), PlanId = "enterprise", FeatureKey = StandardFeatures.CustomDomain, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c4444444-4444-4444-4444-444444444444"), PlanId = "enterprise", FeatureKey = StandardFeatures.SsoEnabled, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c5555555-5555-5555-5555-555555555555"), PlanId = "enterprise", FeatureKey = StandardFeatures.MaxUsers, IsEnabled = true, Limit = -1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c6666666-6666-6666-6666-666666666666"), PlanId = "enterprise", FeatureKey = StandardFeatures.MaxApiKeys, IsEnabled = true, Limit = -1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c7777777-7777-7777-7777-777777777777"), PlanId = "enterprise", FeatureKey = StandardFeatures.MaxWebhooks, IsEnabled = true, Limit = -1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c8888888-8888-8888-8888-888888888888"), PlanId = "enterprise", FeatureKey = StandardFeatures.AdvancedAnalytics, IsEnabled = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.Parse("c9999999-9999-9999-9999-999999999999"), PlanId = "enterprise", FeatureKey = StandardFeatures.PrioritySupport, IsEnabled = true, CreatedAt = DateTime.UtcNow },
        };
        modelBuilder.Entity<PlanFeature>().HasData(planFeatures);
    }
}
