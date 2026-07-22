using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Tenant.Service.Domain.Entities;
using SaaS.Tenant.Service.Infrastructure.Persistence;

namespace SaaS.Tenant.Service.Features.Tenants;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tenants");

        group.MapPost("/", async (CreateTenantRequest request, TenantDbContext db) =>
        {
            if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug))
            {
                return Results.Conflict("Tenant slug already exists.");
            }

            var tenantResult = Domain.Entities.Tenant.Create(request.Name, request.Slug, request.Email);
            
            if (tenantResult.IsFailure)
            {
                return Results.BadRequest(tenantResult.Error);
            }

            db.Tenants.Add(tenantResult.Value);
            await db.SaveChangesAsync();

            return Results.Created($"/tenants/{tenantResult.Value.Id}", new TenantDto(
                tenantResult.Value.Id,
                tenantResult.Value.Name,
                tenantResult.Value.Slug,
                tenantResult.Value.ContactEmail,
                tenantResult.Value.SubscriptionPlan,
                tenantResult.Value.IsActive,
                tenantResult.Value.CreatedAt
            ));
        });

        group.MapGet("/", async (TenantDbContext db, bool? activeOnly = null) =>
        {
            var query = db.Tenants.AsQueryable();
            
            if (activeOnly == true)
            {
                query = query.Where(t => t.IsActive);
            }

            var tenants = await query
                .Select(t => new TenantDto(
                    t.Id,
                    t.Name,
                    t.Slug,
                    t.ContactEmail,
                    t.SubscriptionPlan,
                    t.IsActive,
                    t.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(tenants);
        });

        group.MapGet("/{id:guid}", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.ContactEmail,
                tenant.SubscriptionPlan,
                tenant.IsActive,
                tenant.CreatedAt
            ));
        });

        group.MapGet("/by-slug/{slug}", async (string slug, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.ContactEmail,
                tenant.SubscriptionPlan,
                tenant.IsActive,
                tenant.CreatedAt
            ));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                tenant.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != tenant.Slug)
            {
                if (await db.Tenants.AnyAsync(t => t.Slug == request.Slug && t.Id != id))
                {
                    return Results.Conflict("Tenant slug already exists.");
                }
                tenant.Slug = request.Slug.ToLowerInvariant();
            }

            if (request.Email != null)
            {
                tenant.ContactEmail = request.Email;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug,
                tenant.ContactEmail,
                tenant.SubscriptionPlan,
                tenant.IsActive,
                tenant.CreatedAt
            ));
        });

        group.MapPost("/{id:guid}/deactivate", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            tenant.IsActive = false;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            tenant.IsActive = true;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapDelete("/{id:guid}", async (Guid id, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant is null)
            {
                return Results.NotFound();
            }

            var settings = await db.Settings.Where(s => s.TenantId == id).ToListAsync();
            db.Settings.RemoveRange(settings);
            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        var settingsGroup = app.MapGroup("/tenants/{tenantId:guid}/settings");

        settingsGroup.MapGet("/", async (Guid tenantId, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound("Tenant not found");
            }

            var settings = await db.Settings
                .Where(s => s.TenantId == tenantId)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return Results.Ok(settings);
        });

        settingsGroup.MapPut("/", async (Guid tenantId, Dictionary<string, string> settings, TenantDbContext db) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return Results.NotFound("Tenant not found");
            }

            var existingSettings = await db.Settings
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();

            foreach (var (key, value) in settings)
            {
                var existing = existingSettings.FirstOrDefault(s => s.Key == key);
                if (existing != null)
                {
                    existing.Value = value;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.Settings.Add(TenantSettings.Create(tenantId, key, value));
                }
            }

            await db.SaveChangesAsync();

            var updatedSettings = await db.Settings
                .Where(s => s.TenantId == tenantId)
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return Results.Ok(updatedSettings);
        });

        settingsGroup.MapDelete("/{key}", async (Guid tenantId, string key, TenantDbContext db) =>
        {
            var setting = await db.Settings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Key == key);
            if (setting is null)
            {
                return Results.NotFound();
            }

            db.Settings.Remove(setting);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    public record CreateTenantRequest(string Name, string Slug, string? Email);
    public record UpdateTenantRequest(string? Name, string? Slug, string? Email);
    public record TenantDto(Guid Id, string Name, string Slug, string? Email, string SubscriptionPlan, bool IsActive, DateTime CreatedAt);
}
