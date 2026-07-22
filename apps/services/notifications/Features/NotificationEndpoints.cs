using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Persistence;
using SaaS.Notifications.Service.Services;

namespace SaaS.Notifications.Service.Features;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications");

        group.MapPost("/send", async (
            SendNotificationRequest request,
            NotificationService notificationService) =>
        {
            if (!string.IsNullOrEmpty(request.TemplateKey))
            {
                await notificationService.SendTemplatedEmailAsync(
                    request.TemplateKey,
                    request.Recipient,
                    request.Placeholders ?? new Dictionary<string, string>(),
                    request.TenantId
                );
            }
            else
            {
                await notificationService.SendEmailAsync(
                    request.Recipient,
                    request.Subject ?? "Notification",
                    request.Body ?? "",
                    request.IsHtml ?? true,
                    request.TenantId
                );
            }

            return Results.Ok(new { Message = "Notification queued" });
        });

        group.MapGet("/logs", async (
            NotificationsDbContext db,
            HttpContext httpContext,
            string? status = null,
            int page = 1,
            int pageSize = 50) =>
        {
            var tenantIdHeader = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            Guid? tenantId = null;
            if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tid))
            {
                tenantId = tid;
            }

            var query = db.NotificationLogs.AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(n => n.TenantId == tenantId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(n => n.Status == status);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationLogDto(
                    n.Id,
                    n.Recipient,
                    n.Subject,
                    n.Channel,
                    n.Status,
                    n.TemplateKey,
                    n.CreatedAt,
                    n.SentAt
                ))
                .ToListAsync();

            return Results.Ok(new { Items = logs, TotalCount = totalCount, Page = page, PageSize = pageSize });
        });

        var templatesGroup = app.MapGroup("/templates");

        templatesGroup.MapGet("/", async (NotificationsDbContext db) =>
        {
            var templates = await db.Templates
                .Select(t => new TemplateDto(t.Id, t.Key, t.Name, t.Subject, t.IsHtml, t.IsActive))
                .ToListAsync();
            return Results.Ok(templates);
        });

        templatesGroup.MapGet("/{id:guid}", async (Guid id, NotificationsDbContext db) =>
        {
            var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == id);
            if (template is null) return Results.NotFound();

            return Results.Ok(new TemplateDetailDto(
                template.Id,
                template.Key,
                template.Name,
                template.Subject,
                template.Body,
                template.IsHtml,
                template.IsActive
            ));
        });

        templatesGroup.MapPost("/", async (CreateTemplateRequest request, NotificationsDbContext db) =>
        {
            if (await db.Templates.AnyAsync(t => t.Key == request.Key))
            {
                return Results.Conflict("Template key already exists");
            }

            var template = NotificationTemplate.Create(
                request.Key,
                request.Name,
                request.Subject,
                request.Body,
                request.IsHtml ?? true
            );

            db.Templates.Add(template);
            await db.SaveChangesAsync();

            return Results.Created($"/templates/{template.Id}", template);
        });

        templatesGroup.MapPut("/{id:guid}", async (Guid id, UpdateTemplateRequest request, NotificationsDbContext db) =>
        {
            var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == id);
            if (template is null) return Results.NotFound();

            if (!string.IsNullOrEmpty(request.Name)) template.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Subject)) template.Subject = request.Subject;
            if (!string.IsNullOrEmpty(request.Body)) template.Body = request.Body;
            if (request.IsHtml.HasValue) template.IsHtml = request.IsHtml.Value;
            if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;
            template.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(template);
        });

        templatesGroup.MapDelete("/{id:guid}", async (Guid id, NotificationsDbContext db) =>
        {
            var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == id);
            if (template is null) return Results.NotFound();

            db.Templates.Remove(template);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    public record SendNotificationRequest(
        string Recipient,
        string? TemplateKey = null,
        string? Subject = null,
        string? Body = null,
        bool? IsHtml = null,
        Guid? TenantId = null,
        Dictionary<string, string>? Placeholders = null
    );

    public record NotificationLogDto(
        Guid Id,
        string Recipient,
        string Subject,
        string Channel,
        string Status,
        string? TemplateKey,
        DateTime CreatedAt,
        DateTime? SentAt
    );

    public record TemplateDto(Guid Id, string Key, string Name, string Subject, bool IsHtml, bool IsActive);
    public record TemplateDetailDto(Guid Id, string Key, string Name, string Subject, string Body, bool IsHtml, bool IsActive);
    public record CreateTemplateRequest(string Key, string Name, string Subject, string Body, bool? IsHtml);
    public record UpdateTemplateRequest(string? Name, string? Subject, string? Body, bool? IsHtml, bool? IsActive);
}
