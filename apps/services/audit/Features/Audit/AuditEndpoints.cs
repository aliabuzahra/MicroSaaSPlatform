using Microsoft.EntityFrameworkCore;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure.Persistence;

namespace SaaS.Audit.Service.Features.Audit;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit");

        group.MapGet("/logs", async (
            AuditDbContext db,
            string? eventType,
            string? action,
            Guid? userId,
            Guid? tenantId,
            string? resourceType,
            string? resourceId,
            string? severity,
            DateTime? startDate,
            DateTime? endDate,
            int? page,
            int? pageSize) =>
        {
            var query = db.AuditLogs.AsQueryable();
            
            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(a => a.EventType == eventType);
            
            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action == action);
            
            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);
            
            if (tenantId.HasValue)
                query = query.Where(a => a.TenantId == tenantId.Value);
            
            if (!string.IsNullOrEmpty(resourceType))
                query = query.Where(a => a.ResourceType == resourceType);
            
            if (!string.IsNullOrEmpty(resourceId))
                query = query.Where(a => a.ResourceId == resourceId);
            
            if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AuditSeverity>(severity, true, out var severityEnum))
                query = query.Where(a => a.Severity == severityEnum);
            
            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 50, 200);
            
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(a => new AuditLogResponse(a))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<AuditLogResponse>(logs, totalCount, actualPage, actualPageSize));
        });

        group.MapGet("/logs/{id:guid}", async (Guid id, AuditDbContext db) =>
        {
            var log = await db.AuditLogs.FirstOrDefaultAsync(a => a.Id == id);
            
            if (log is null)
                return Results.NotFound(new { Error = "Audit log not found." });

            return Results.Ok(new AuditLogResponse(log));
        });

        group.MapGet("/logs/resource/{resourceType}/{resourceId}", async (
            string resourceType,
            string resourceId,
            AuditDbContext db,
            int? page,
            int? pageSize) =>
        {
            var query = db.AuditLogs
                .Where(a => a.ResourceType == resourceType && a.ResourceId == resourceId);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 50, 200);
            
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(a => new AuditLogResponse(a))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<AuditLogResponse>(logs, totalCount, actualPage, actualPageSize));
        });

        group.MapGet("/logs/user/{userId:guid}", async (
            Guid userId,
            AuditDbContext db,
            int? page,
            int? pageSize) =>
        {
            var query = db.AuditLogs.Where(a => a.UserId == userId);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 50, 200);
            
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(a => new AuditLogResponse(a))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<AuditLogResponse>(logs, totalCount, actualPage, actualPageSize));
        });

        group.MapGet("/logs/tenant/{tenantId:guid}", async (
            Guid tenantId,
            AuditDbContext db,
            int? page,
            int? pageSize) =>
        {
            var query = db.AuditLogs.Where(a => a.TenantId == tenantId);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 50, 200);
            
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(a => new AuditLogResponse(a))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<AuditLogResponse>(logs, totalCount, actualPage, actualPageSize));
        });

        group.MapGet("/stats", async (AuditDbContext db, int? days) =>
        {
            var lookbackDays = days ?? 30;
            var startDate = DateTime.UtcNow.AddDays(-lookbackDays);
            
            var stats = await db.AuditLogs
                .Where(a => a.Timestamp >= startDate)
                .GroupBy(a => a.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToListAsync();

            var severityStats = await db.AuditLogs
                .Where(a => a.Timestamp >= startDate)
                .GroupBy(a => a.Severity)
                .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var totalLogs = await db.AuditLogs
                .Where(a => a.Timestamp >= startDate)
                .CountAsync();

            return Results.Ok(new
            {
                Period = $"Last {lookbackDays} days",
                TotalLogs = totalLogs,
                ByEventType = stats,
                BySeverity = severityStats
            });
        });

        group.MapGet("/event-types", () =>
        {
            var eventTypes = typeof(AuditEventTypes)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.GetValue(null)?.ToString())
                .Where(v => v != null)
                .ToList();

            return Results.Ok(eventTypes);
        });
    }

    public record AuditLogResponse(
        Guid Id,
        string EventType,
        string Action,
        Guid? UserId,
        string? UserEmail,
        Guid? TenantId,
        string? ResourceType,
        string? ResourceId,
        string? OldValues,
        string? NewValues,
        string? IpAddress,
        string? CorrelationId,
        string? Metadata,
        string Severity,
        DateTime Timestamp)
    {
        public AuditLogResponse(AuditLog a) : this(
            a.Id, a.EventType, a.Action, a.UserId, a.UserEmail, a.TenantId, 
            a.ResourceType, a.ResourceId, a.OldValues, a.NewValues, a.IpAddress,
            a.CorrelationId, a.Metadata, a.Severity.ToString(), a.Timestamp)
        { }
    }

    public record PaginatedResponse<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
