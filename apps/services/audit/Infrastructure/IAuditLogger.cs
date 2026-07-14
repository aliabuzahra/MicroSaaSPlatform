using SaaS.Audit.Service.Domain.Entities;

namespace SaaS.Audit.Service.Infrastructure;

public interface IAuditLogger
{
    Task LogAsync(
        string eventType,
        string action,
        Guid? userId = null,
        string? userEmail = null,
        Guid? tenantId = null,
        string? resourceType = null,
        string? resourceId = null,
        object? oldValues = null,
        object? newValues = null,
        object? metadata = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken cancellationToken = default);
}
