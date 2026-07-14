using Microsoft.EntityFrameworkCore;
using SaaS.Webhooks.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Webhooks.Service.Infrastructure.Persistence;

public class WebhooksDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<WebhookSubscription> Subscriptions { get; set; }
    public DbSet<WebhookDelivery> Deliveries { get; set; }

    public WebhooksDbContext(DbContextOptions<WebhooksDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebhookSubscription>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Url).IsRequired().HasMaxLength(500);
            builder.Property(s => s.Secret).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Events).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            
            builder.Property(s => s.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasIndex(s => s.TenantId);
            builder.HasQueryFilter(s => s.TenantId == _tenantContext.TenantId);
            builder.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<WebhookDelivery>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.EventType).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Payload).IsRequired();
            builder.Property(d => d.Url).IsRequired().HasMaxLength(500);
            builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(d => d.ErrorMessage).HasMaxLength(2000);
            
            builder.HasIndex(d => d.SubscriptionId);
            builder.HasIndex(d => d.TenantId);
            builder.HasIndex(d => new { d.Status, d.NextRetryAt });
            builder.Ignore(d => d.DomainEvents);
        });
    }
}
