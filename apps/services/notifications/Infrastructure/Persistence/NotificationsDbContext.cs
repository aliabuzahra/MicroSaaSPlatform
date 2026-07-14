using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;

namespace SaaS.Notifications.Service.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.UserId).IsRequired();
            builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
            builder.Property(n => n.Channel).IsRequired().HasMaxLength(50);
            builder.Property(n => n.Recipient).IsRequired().HasMaxLength(255);
            builder.Property(n => n.Subject).IsRequired().HasMaxLength(500);
            builder.Property(n => n.Body).IsRequired();
            builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(n => n.FailureReason).HasMaxLength(1000);
            builder.Property(n => n.ExternalId).HasMaxLength(255);
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.TenantId);
            builder.HasIndex(n => new { n.Status, n.CreatedAt });
            builder.Ignore(n => n.DomainEvents);
        });

        modelBuilder.Entity<NotificationTemplate>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Type).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Channel).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
            builder.Property(t => t.BodyTemplate).IsRequired();
            builder.HasIndex(t => new { t.Type, t.Channel }).IsUnique();
            builder.Ignore(t => t.DomainEvents);
        });

        modelBuilder.Entity<UserNotificationPreference>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.NotificationType).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => new { p.UserId, p.NotificationType }).IsUnique();
            builder.Ignore(p => p.DomainEvents);
        });

        SeedTemplates(modelBuilder);
    }

    private static void SeedTemplates(ModelBuilder modelBuilder)
    {
        var templates = new List<NotificationTemplate>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Welcome Email",
                Type = NotificationTypes.WelcomeEmail,
                Channel = NotificationChannels.Email,
                Subject = "Welcome to SaaS Platform, {{name}}!",
                BodyTemplate = @"
<h1>Welcome, {{name}}!</h1>
<p>Thank you for joining SaaS Platform. We're excited to have you on board.</p>
<p>Your account has been created successfully with the email: {{email}}</p>
<p>Get started by exploring your dashboard.</p>
<p>Best regards,<br/>The SaaS Platform Team</p>",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Password Reset",
                Type = NotificationTypes.PasswordReset,
                Channel = NotificationChannels.Email,
                Subject = "Reset Your Password",
                BodyTemplate = @"
<h1>Password Reset Request</h1>
<p>Hi {{name}},</p>
<p>We received a request to reset your password. Click the link below to create a new password:</p>
<p><a href=""{{reset_url}}"">Reset Password</a></p>
<p>This link will expire in 24 hours.</p>
<p>If you didn't request this, please ignore this email.</p>
<p>Best regards,<br/>The SaaS Platform Team</p>",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Email Verification",
                Type = NotificationTypes.EmailVerification,
                Channel = NotificationChannels.Email,
                Subject = "Verify Your Email Address",
                BodyTemplate = @"
<h1>Verify Your Email</h1>
<p>Hi {{name}},</p>
<p>Please verify your email address by clicking the link below:</p>
<p><a href=""{{verification_url}}"">Verify Email</a></p>
<p>This link will expire in 48 hours.</p>
<p>Best regards,<br/>The SaaS Platform Team</p>",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Tenant Invitation",
                Type = NotificationTypes.TenantInvitation,
                Channel = NotificationChannels.Email,
                Subject = "You've been invited to join {{tenant_name}}",
                BodyTemplate = @"
<h1>You're Invited!</h1>
<p>Hi there,</p>
<p>You've been invited to join <strong>{{tenant_name}}</strong> as a {{role}}.</p>
<p>Click the link below to accept the invitation:</p>
<p><a href=""{{invitation_url}}"">Accept Invitation</a></p>
<p>This invitation will expire in 7 days.</p>
<p>Best regards,<br/>The SaaS Platform Team</p>",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Subscription Updated",
                Type = NotificationTypes.SubscriptionUpdated,
                Channel = NotificationChannels.Email,
                Subject = "Your Subscription Has Been Updated",
                BodyTemplate = @"
<h1>Subscription Update</h1>
<p>Hi {{name}},</p>
<p>Your subscription status has been updated to: <strong>{{status}}</strong></p>
<p>Plan: {{plan}}</p>
<p>Next billing date: {{next_bill_date}}</p>
<p>If you have any questions, please contact support.</p>
<p>Best regards,<br/>The SaaS Platform Team</p>",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<NotificationTemplate>().HasData(templates);
    }
}
