using Microsoft.EntityFrameworkCore;
using SaaS.Config.Service.Domain.Entities;
using SaaS.Config.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Config.Service.Features.Settings;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var tenantGroup = app.MapGroup("/api/config/tenant").RequireAuthorization();
        var globalGroup = app.MapGroup("/api/config/global");

        tenantGroup.MapGet("/", async (HttpContext context, ConfigDbContext db) =>
        {
            var settings = await db.TenantSettings.ToListAsync();
            return Results.Ok(settings
                .Select(s => new SettingResponse(s.Key, s.IsSecret ? "***" : s.Value, s.Type.ToString()))
                .ToDictionary(s => s.Key, s => s));
        });

        tenantGroup.MapGet("/{key}", async (string key, HttpContext context, ConfigDbContext db) =>
        {
            var setting = await db.TenantSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting is null)
                return Results.NotFound(new { Error = "Setting not found." });

            return Results.Ok(new SettingResponse(setting.Key, setting.IsSecret ? "***" : setting.Value, setting.Type.ToString()));
        });

        tenantGroup.MapPut("/{key}", async (
            string key,
            SetSettingRequest request,
            HttpContext context,
            ConfigDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Results.Unauthorized();

            var setting = await db.TenantSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting is null)
            {
                setting = TenantSettings.Create(
                    tenantId.Value, 
                    key, 
                    request.Value, 
                    request.Type ?? SettingType.String,
                    request.IsSecret ?? false);
                db.TenantSettings.Add(setting);
            }
            else
            {
                setting.Update(request.Value);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Setting saved." });
        });

        tenantGroup.MapDelete("/{key}", async (string key, ConfigDbContext db) =>
        {
            var setting = await db.TenantSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting is null)
                return Results.NotFound(new { Error = "Setting not found." });

            db.TenantSettings.Remove(setting);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Setting deleted." });
        });

        tenantGroup.MapPost("/bulk", async (
            BulkSettingsRequest request,
            HttpContext context,
            ConfigDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Results.Unauthorized();

            foreach (var item in request.Settings)
            {
                var existing = await db.TenantSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
                if (existing != null)
                {
                    existing.Update(item.Value);
                }
                else
                {
                    var setting = TenantSettings.Create(tenantId.Value, item.Key, item.Value);
                    db.TenantSettings.Add(setting);
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = $"{request.Settings.Count} settings saved." });
        });

        globalGroup.MapGet("/", async (ConfigDbContext db) =>
        {
            var settings = await db.GlobalSettings.ToListAsync();
            return Results.Ok(settings
                .Select(s => new GlobalSettingResponse(s.Key, s.IsSecret ? "***" : s.Value, s.Type.ToString(), s.Description))
                .ToDictionary(s => s.Key, s => s));
        });

        globalGroup.MapGet("/{key}", async (string key, ConfigDbContext db) =>
        {
            var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting is null)
                return Results.NotFound(new { Error = "Setting not found." });

            return Results.Ok(new GlobalSettingResponse(setting.Key, setting.IsSecret ? "***" : setting.Value, setting.Type.ToString(), setting.Description));
        });

        globalGroup.MapPut("/{key}", async (string key, SetGlobalSettingRequest request, ConfigDbContext db) =>
        {
            var setting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting is null)
            {
                setting = GlobalSettings.Create(key, request.Value, request.Type ?? SettingType.String, request.Description, request.IsSecret ?? false);
                db.GlobalSettings.Add(setting);
            }
            else
            {
                setting.Value = request.Value;
                if (request.Description != null) setting.Description = request.Description;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Setting saved." });
        }).RequireAuthorization();
    }

    public record SetSettingRequest(string Value, SettingType? Type, bool? IsSecret);
    public record SetGlobalSettingRequest(string Value, SettingType? Type, string? Description, bool? IsSecret);
    public record BulkSettingsRequest(List<KeyValuePair<string, string>> Settings);

    public record SettingResponse(string Key, string Value, string Type);
    public record GlobalSettingResponse(string Key, string Value, string Type, string? Description);
}
