using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Audit.Service.Domain.Entities;

public class AuditLog : Entity<Guid>
{
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AuditLog Create(
        Guid tenantId,
        string action,
        string entityType,
        string? entityId = null,
        Guid? userId = null,
        string? userEmail = null,
        string? details = null,
        string? ipAddress = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            UserEmail = userEmail,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
    }
}
