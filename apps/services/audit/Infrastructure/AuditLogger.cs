using System.Text.Json;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure.Persistence;

namespace SaaS.Audit.Service.Infrastructure;

public class AuditLogger : IAuditLogger
{
    private readonly AuditDbContext _db;
    private readonly ILogger<AuditLogger> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditLogger(AuditDbContext db, ILogger<AuditLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = AuditLog.Create(
                eventType,
                action,
                userId,
                userEmail,
                tenantId,
                resourceType,
                resourceId,
                oldValues != null ? JsonSerializer.Serialize(oldValues, JsonOptions) : null,
                newValues != null ? JsonSerializer.Serialize(newValues, JsonOptions) : null,
                metadata != null ? JsonSerializer.Serialize(metadata, JsonOptions) : null,
                severity);

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: {EventType} - {Action} by user {UserId} on {ResourceType}/{ResourceId}",
                eventType, action, userId, resourceType, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create audit log for event {EventType} - {Action}", 
                eventType, action);
            throw;
        }
    }
}
