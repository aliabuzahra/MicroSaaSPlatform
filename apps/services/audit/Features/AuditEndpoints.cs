using Microsoft.EntityFrameworkCore;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure.Persistence;

namespace SaaS.Audit.Service.Features;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/audit/logs");

        group.MapGet("/", async (
            AuditDbContext db,
            HttpContext httpContext,
            string? entityType = null,
            string? action = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50) =>
        {
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantIdHeader) || !Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                return Results.BadRequest("Tenant ID is required");
            }

            var query = db.AuditLogs
                .Where(a => a.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.Timestamp <= to.Value);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto(
                    a.Id,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.UserId,
                    a.UserEmail,
                    a.Details,
                    a.IpAddress,
                    a.Timestamp
                ))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<AuditLogDto>(logs, totalCount, page, pageSize));
        });

        group.MapGet("/{id:guid}", async (Guid id, AuditDbContext db, HttpContext httpContext) =>
        {
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantIdHeader) || !Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                return Results.BadRequest("Tenant ID is required");
            }

            var log = await db.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);

            if (log is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new AuditLogDto(
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.UserId,
                log.UserEmail,
                log.Details,
                log.IpAddress,
                log.Timestamp
            ));
        });

        group.MapGet("/entity-types", async (AuditDbContext db, HttpContext httpContext) =>
        {
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantIdHeader) || !Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                return Results.BadRequest("Tenant ID is required");
            }

            var types = await db.AuditLogs
                .Where(a => a.TenantId == tenantId)
                .Select(a => a.EntityType)
                .Distinct()
                .ToListAsync();

            return Results.Ok(types);
        });

        group.MapGet("/actions", async (AuditDbContext db, HttpContext httpContext) =>
        {
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantIdHeader) || !Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                return Results.BadRequest("Tenant ID is required");
            }

            var actions = await db.AuditLogs
                .Where(a => a.TenantId == tenantId)
                .Select(a => a.Action)
                .Distinct()
                .ToListAsync();

            return Results.Ok(actions);
        });
    }

    public record AuditLogDto(
        Guid Id,
        string Action,
        string EntityType,
        string? EntityId,
        Guid? UserId,
        string? UserEmail,
        string? Details,
        string? IpAddress,
        DateTime Timestamp
    );

    public record PaginatedResponse<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int Page,
        int PageSize
    )
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
