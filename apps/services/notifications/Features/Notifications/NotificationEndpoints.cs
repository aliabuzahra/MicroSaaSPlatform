using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Persistence;

namespace SaaS.Notifications.Service.Features.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var notificationsGroup = app.MapGroup("/api/notifications");
        var preferencesGroup = app.MapGroup("/api/notifications/preferences");
        var templatesGroup = app.MapGroup("/api/notifications/templates");

        notificationsGroup.MapGet("/", async (
            NotificationsDbContext db,
            Guid? userId,
            Guid? tenantId,
            string? channel,
            string? status,
            int? page,
            int? pageSize) =>
        {
            var query = db.Notifications.AsQueryable();
            
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);
            
            if (tenantId.HasValue)
                query = query.Where(n => n.TenantId == tenantId.Value);
            
            if (!string.IsNullOrEmpty(channel))
                query = query.Where(n => n.Channel == channel);
            
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<NotificationStatus>(status, true, out var statusEnum))
                query = query.Where(n => n.Status == statusEnum);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 20, 100);
            
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(n => new NotificationResponse(n))
                .ToListAsync();

            return Results.Ok(new PaginatedResponse<NotificationResponse>(notifications, totalCount, actualPage, actualPageSize));
        });

        notificationsGroup.MapGet("/{id:guid}", async (Guid id, NotificationsDbContext db) =>
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            
            if (notification is null)
                return Results.NotFound(new { Error = "Notification not found." });

            return Results.Ok(new NotificationResponse(notification));
        });

        notificationsGroup.MapPost("/{id:guid}/read", async (Guid id, NotificationsDbContext db) =>
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            
            if (notification is null)
                return Results.NotFound(new { Error = "Notification not found." });

            notification.MarkAsRead();
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Notification marked as read." });
        });

        notificationsGroup.MapGet("/user/{userId:guid}/unread-count", async (Guid userId, NotificationsDbContext db) =>
        {
            var count = await db.Notifications
                .Where(n => n.UserId == userId && n.ReadAt == null && n.Status == NotificationStatus.Sent)
                .CountAsync();

            return Results.Ok(new { UnreadCount = count });
        });

        preferencesGroup.MapGet("/{userId:guid}", async (Guid userId, NotificationsDbContext db) =>
        {
            var preferences = await db.UserNotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return Results.Ok(preferences.Select(p => new PreferenceResponse(p)));
        });

        preferencesGroup.MapPut("/{userId:guid}", async (
            Guid userId, 
            UpdatePreferencesRequest request, 
            NotificationsDbContext db) =>
        {
            foreach (var pref in request.Preferences)
            {
                var existing = await db.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == pref.NotificationType);

                if (existing is null)
                {
                    existing = UserNotificationPreference.CreateDefault(userId, pref.NotificationType);
                    db.UserNotificationPreferences.Add(existing);
                }

                existing.UpdatePreference(pref.EmailEnabled, pref.SmsEnabled, pref.PushEnabled, pref.InAppEnabled);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Preferences updated." });
        });

        templatesGroup.MapGet("/", async (NotificationsDbContext db) =>
        {
            var templates = await db.NotificationTemplates
                .OrderBy(t => t.Type)
                .Select(t => new TemplateResponse(t))
                .ToListAsync();

            return Results.Ok(templates);
        });

        templatesGroup.MapGet("/{id:guid}", async (Guid id, NotificationsDbContext db) =>
        {
            var template = await db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id);
            
            if (template is null)
                return Results.NotFound(new { Error = "Template not found." });

            return Results.Ok(new TemplateResponse(template));
        });

        templatesGroup.MapPost("/", async (CreateTemplateRequest request, NotificationsDbContext db) =>
        {
            var existing = await db.NotificationTemplates
                .AnyAsync(t => t.Type == request.Type && t.Channel == request.Channel);

            if (existing)
                return Results.Conflict(new { Error = "A template with this type and channel already exists." });

            var template = NotificationTemplate.Create(
                request.Name,
                request.Type,
                request.Channel,
                request.Subject,
                request.BodyTemplate);

            db.NotificationTemplates.Add(template);
            await db.SaveChangesAsync();

            return Results.Created($"/api/notifications/templates/{template.Id}", new TemplateResponse(template));
        });

        templatesGroup.MapPut("/{id:guid}", async (Guid id, UpdateTemplateRequest request, NotificationsDbContext db) =>
        {
            var template = await db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id);
            
            if (template is null)
                return Results.NotFound(new { Error = "Template not found." });

            if (request.Name is not null)
                template.Name = request.Name;
            if (request.Subject is not null)
                template.Subject = request.Subject;
            if (request.BodyTemplate is not null)
                template.BodyTemplate = request.BodyTemplate;
            if (request.IsActive.HasValue)
                template.IsActive = request.IsActive.Value;

            template.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new TemplateResponse(template));
        });
    }

    public record NotificationResponse(
        Guid Id,
        Guid UserId,
        Guid? TenantId,
        string Type,
        string Channel,
        string Recipient,
        string Subject,
        string Status,
        DateTime CreatedAt,
        DateTime? SentAt,
        DateTime? ReadAt,
        string? FailureReason)
    {
        public NotificationResponse(Notification n) : this(
            n.Id, n.UserId, n.TenantId, n.Type, n.Channel, n.Recipient, n.Subject,
            n.Status.ToString(), n.CreatedAt, n.SentAt, n.ReadAt, n.FailureReason)
        { }
    }

    public record PreferenceResponse(
        Guid Id,
        Guid UserId,
        string NotificationType,
        bool EmailEnabled,
        bool SmsEnabled,
        bool PushEnabled,
        bool InAppEnabled)
    {
        public PreferenceResponse(UserNotificationPreference p) : this(
            p.Id, p.UserId, p.NotificationType, p.EmailEnabled, p.SmsEnabled, p.PushEnabled, p.InAppEnabled)
        { }
    }

    public record TemplateResponse(
        Guid Id,
        string Name,
        string Type,
        string Channel,
        string Subject,
        string BodyTemplate,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt)
    {
        public TemplateResponse(NotificationTemplate t) : this(
            t.Id, t.Name, t.Type, t.Channel, t.Subject, t.BodyTemplate, t.IsActive, t.CreatedAt, t.UpdatedAt)
        { }
    }

    public record UpdatePreferencesRequest(List<PreferenceUpdate> Preferences);
    public record PreferenceUpdate(string NotificationType, bool? EmailEnabled, bool? SmsEnabled, bool? PushEnabled, bool? InAppEnabled);
    public record CreateTemplateRequest(string Name, string Type, string Channel, string Subject, string BodyTemplate);
    public record UpdateTemplateRequest(string? Name, string? Subject, string? BodyTemplate, bool? IsActive);

    public record PaginatedResponse<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
