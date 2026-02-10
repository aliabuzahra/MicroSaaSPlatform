using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Domain.Entities;

namespace SaaS.Billing.Service.Infrastructure.Persistence;

public class BillingDbContext : DbContext
{
    public DbSet<BillingPlan> Plans { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<UsageRecord> UsageRecords { get; set; }
    public DbSet<PlanLimit> PlanLimits { get; set; }

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        
        modelBuilder.Entity<BillingPlan>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.StripePriceId).IsRequired().HasMaxLength(100);
            builder.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<Subscription>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.PaddleSubscriptionId).IsRequired().HasMaxLength(100);
            builder.Ignore(s => s.DomainEvents);
        });
    }
}
