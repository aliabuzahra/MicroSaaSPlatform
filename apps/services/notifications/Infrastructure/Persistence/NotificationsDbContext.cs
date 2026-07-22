using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;

namespace SaaS.Notifications.Service.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext
{
    public DbSet<NotificationTemplate> Templates { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Key).IsRequired().HasMaxLength(100);
            builder.HasIndex(t => t.Key).IsUnique();
            builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
            builder.Property(t => t.Body).IsRequired();
            builder.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<NotificationLog>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Recipient).IsRequired().HasMaxLength(255);
            builder.Property(n => n.Subject).IsRequired().HasMaxLength(500);
            builder.Property(n => n.Channel).IsRequired().HasMaxLength(50);
            builder.Property(n => n.Status).IsRequired().HasMaxLength(50);
            builder.Property(n => n.TemplateKey).HasMaxLength(100);
            builder.HasIndex(n => n.TenantId);
            builder.HasIndex(n => n.Status);
            builder.HasIndex(n => n.CreatedAt);
            builder.Ignore(n => n.DomainEvents);
        });

        SeedTemplates(modelBuilder);
    }

    private static void SeedTemplates(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Key = "welcome_email",
                Name = "Welcome Email",
                Subject = "Welcome to Multi Micro SaaS, {{Name}}!",
                Body = @"
                    <h1>Welcome, {{Name}}!</h1>
                    <p>Thank you for registering with Multi Micro SaaS.</p>
                    <p>Your account has been created successfully with email: {{Email}}</p>
                    <p>Get started by exploring our features.</p>
                ",
                IsHtml = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Key = "subscription_activated",
                Name = "Subscription Activated",
                Subject = "Your subscription is now active!",
                Body = @"
                    <h1>Subscription Activated</h1>
                    <p>Your subscription to plan {{PlanId}} is now active.</p>
                    <p>Next billing date: {{NextBillDate}}</p>
                ",
                IsHtml = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new NotificationTemplate
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Key = "subscription_cancelled",
                Name = "Subscription Cancelled",
                Subject = "Your subscription has been cancelled",
                Body = @"
                    <h1>Subscription Cancelled</h1>
                    <p>Your subscription has been cancelled.</p>
                    <p>We're sorry to see you go. You can reactivate anytime.</p>
                ",
                IsHtml = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
