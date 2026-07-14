using Microsoft.EntityFrameworkCore;
using SaaS.Identity.Service.Domain.Entities;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

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
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.EmailVerified).HasDefaultValue(false);
            builder.Property(u => u.LastLoginAt);
            builder.Ignore(u => u.DomainEvents);

            builder.Property(u => u.TenantId)
                .HasConversion(id => id.Value, value => new TenantId(value));
            
            builder.HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Token).IsRequired().HasMaxLength(256);
            builder.HasIndex(t => t.Token).IsUnique();
            builder.Property(t => t.UserId).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();
            builder.Property(t => t.ReplacedByToken).HasMaxLength(256);
            builder.Ignore(t => t.DomainEvents);
            
            builder.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Token).IsRequired().HasMaxLength(256);
            builder.HasIndex(t => t.Token).IsUnique();
            builder.Property(t => t.UserId).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();
            builder.Ignore(t => t.DomainEvents);
            
            builder.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<EmailVerificationToken>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Token).IsRequired().HasMaxLength(256);
            builder.HasIndex(t => t.Token).IsUnique();
            builder.Property(t => t.UserId).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();
            builder.Ignore(t => t.DomainEvents);
            
            builder.HasIndex(t => t.UserId);
        });
    }
}
