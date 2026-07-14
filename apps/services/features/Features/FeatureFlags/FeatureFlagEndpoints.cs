using Microsoft.EntityFrameworkCore;
using SaaS.Features.Service.Domain.Entities;
using SaaS.Features.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Features.Service.Features.FeatureFlags;

public static class FeatureFlagEndpoints
{
    public static void MapFeatureFlagEndpoints(this IEndpointRouteBuilder app)
    {
        var flagsGroup = app.MapGroup("/api/features/flags");
        var plansGroup = app.MapGroup("/api/features/plans");
        var overridesGroup = app.MapGroup("/api/features/overrides").RequireAuthorization();
        var checkGroup = app.MapGroup("/api/features/check");

        flagsGroup.MapGet("/", async (FeaturesDbContext db) =>
        {
            var flags = await db.FeatureFlags.OrderBy(f => f.Name).ToListAsync();
            return Results.Ok(flags.Select(f => new FeatureFlagResponse(f)));
        });

        flagsGroup.MapGet("/{key}", async (string key, FeaturesDbContext db) =>
        {
            var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
            if (flag is null)
                return Results.NotFound(new { Error = "Feature flag not found." });
            return Results.Ok(new FeatureFlagResponse(flag));
        });

        flagsGroup.MapPost("/", async (CreateFeatureFlagRequest request, FeaturesDbContext db) =>
        {
            var existing = await db.FeatureFlags.AnyAsync(f => f.Key == request.Key.ToLowerInvariant());
            if (existing)
                return Results.Conflict(new { Error = "Feature flag with this key already exists." });

            var flag = FeatureFlag.Create(request.Key, request.Name, request.Description, request.IsEnabled);
            db.FeatureFlags.Add(flag);
            await db.SaveChangesAsync();

            return Results.Created($"/api/features/flags/{flag.Key}", new FeatureFlagResponse(flag));
        }).RequireAuthorization();

        flagsGroup.MapPut("/{key}", async (string key, UpdateFeatureFlagRequest request, FeaturesDbContext db) =>
        {
            var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
            if (flag is null)
                return Results.NotFound(new { Error = "Feature flag not found." });

            flag.Update(request.Name, request.Description, request.IsEnabled);
            await db.SaveChangesAsync();

            return Results.Ok(new FeatureFlagResponse(flag));
        }).RequireAuthorization();

        plansGroup.MapGet("/{planId}/features", async (string planId, FeaturesDbContext db) =>
        {
            var features = await db.PlanFeatures
                .Where(p => p.PlanId == planId)
                .ToListAsync();

            return Results.Ok(features.Select(f => new PlanFeatureResponse(f)));
        });

        plansGroup.MapPost("/{planId}/features", async (string planId, SetPlanFeatureRequest request, FeaturesDbContext db) =>
        {
            var existing = await db.PlanFeatures
                .FirstOrDefaultAsync(p => p.PlanId == planId && p.FeatureKey == request.FeatureKey);

            if (existing != null)
            {
                existing.IsEnabled = request.IsEnabled;
                existing.Value = request.Value;
                existing.Limit = request.Limit;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var feature = PlanFeature.Create(planId, request.FeatureKey, request.IsEnabled, request.Value, request.Limit);
                db.PlanFeatures.Add(feature);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Plan feature updated." });
        }).RequireAuthorization();

        overridesGroup.MapGet("/tenant/{tenantId:guid}", async (Guid tenantId, FeaturesDbContext db) =>
        {
            var overrides = await db.TenantOverrides
                .Where(o => o.TenantId == new TenantId(tenantId))
                .ToListAsync();

            return Results.Ok(overrides.Select(o => new TenantOverrideResponse(o)));
        });

        overridesGroup.MapPost("/tenant/{tenantId:guid}", async (
            Guid tenantId,
            CreateOverrideRequest request,
            FeaturesDbContext db) =>
        {
            var existing = await db.TenantOverrides
                .FirstOrDefaultAsync(o => o.TenantId == new TenantId(tenantId) && o.FeatureKey == request.FeatureKey);

            if (existing != null)
            {
                existing.IsEnabled = request.IsEnabled;
                existing.Value = request.Value;
                existing.Limit = request.Limit;
                existing.Reason = request.Reason;
                existing.ExpiresAt = request.ExpiresAt;
            }
            else
            {
                var over = TenantFeatureOverride.Create(
                    tenantId, request.FeatureKey, request.IsEnabled, 
                    request.Value, request.Limit, request.Reason, request.ExpiresAt);
                db.TenantOverrides.Add(over);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = "Tenant override saved." });
        });

        overridesGroup.MapDelete("/tenant/{tenantId:guid}/{featureKey}", async (
            Guid tenantId,
            string featureKey,
            FeaturesDbContext db) =>
        {
            var over = await db.TenantOverrides
                .FirstOrDefaultAsync(o => o.TenantId == new TenantId(tenantId) && o.FeatureKey == featureKey);

            if (over is null)
                return Results.NotFound(new { Error = "Override not found." });

            db.TenantOverrides.Remove(over);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Override removed." });
        });

        checkGroup.MapGet("/{tenantId:guid}/{featureKey}", async (
            Guid tenantId,
            string featureKey,
            string? planId,
            FeaturesDbContext db) =>
        {
            var over = await db.TenantOverrides
                .FirstOrDefaultAsync(o => o.TenantId == new TenantId(tenantId) && 
                                          o.FeatureKey == featureKey &&
                                          (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow));

            if (over != null)
            {
                return Results.Ok(new FeatureCheckResponse(featureKey, over.IsEnabled, over.Value, over.Limit, "override"));
            }

            if (!string.IsNullOrEmpty(planId))
            {
                var planFeature = await db.PlanFeatures
                    .FirstOrDefaultAsync(p => p.PlanId == planId && p.FeatureKey == featureKey);

                if (planFeature != null)
                {
                    return Results.Ok(new FeatureCheckResponse(featureKey, planFeature.IsEnabled, planFeature.Value, planFeature.Limit, "plan"));
                }
            }

            var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.Key == featureKey);
            if (flag != null)
            {
                return Results.Ok(new FeatureCheckResponse(featureKey, flag.IsEnabled, flag.DefaultValue, null, "default"));
            }

            return Results.Ok(new FeatureCheckResponse(featureKey, false, null, null, "not_found"));
        });

        checkGroup.MapGet("/{tenantId:guid}", async (Guid tenantId, string? planId, FeaturesDbContext db) =>
        {
            var allFlags = await db.FeatureFlags.ToListAsync();
            var planFeatures = string.IsNullOrEmpty(planId) 
                ? new List<PlanFeature>() 
                : await db.PlanFeatures.Where(p => p.PlanId == planId).ToListAsync();
            var overrides = await db.TenantOverrides
                .Where(o => o.TenantId == new TenantId(tenantId) && (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow))
                .ToListAsync();

            var results = new Dictionary<string, FeatureCheckResponse>();

            foreach (var flag in allFlags)
            {
                var over = overrides.FirstOrDefault(o => o.FeatureKey == flag.Key);
                if (over != null)
                {
                    results[flag.Key] = new FeatureCheckResponse(flag.Key, over.IsEnabled, over.Value, over.Limit, "override");
                    continue;
                }

                var planFeature = planFeatures.FirstOrDefault(p => p.FeatureKey == flag.Key);
                if (planFeature != null)
                {
                    results[flag.Key] = new FeatureCheckResponse(flag.Key, planFeature.IsEnabled, planFeature.Value, planFeature.Limit, "plan");
                    continue;
                }

                results[flag.Key] = new FeatureCheckResponse(flag.Key, flag.IsEnabled, flag.DefaultValue, null, "default");
            }

            return Results.Ok(results);
        });
    }

    public record CreateFeatureFlagRequest(string Key, string Name, string? Description, bool IsEnabled);
    public record UpdateFeatureFlagRequest(string? Name, string? Description, bool? IsEnabled);
    public record SetPlanFeatureRequest(string FeatureKey, bool IsEnabled, string? Value, int? Limit);
    public record CreateOverrideRequest(string FeatureKey, bool IsEnabled, string? Value, int? Limit, string? Reason, DateTime? ExpiresAt);

    public record FeatureFlagResponse(Guid Id, string Key, string Name, string? Description, bool IsEnabled, string Type, DateTime CreatedAt)
    {
        public FeatureFlagResponse(FeatureFlag f) : this(f.Id, f.Key, f.Name, f.Description, f.IsEnabled, f.Type.ToString(), f.CreatedAt) { }
    }

    public record PlanFeatureResponse(Guid Id, string PlanId, string FeatureKey, bool IsEnabled, string? Value, int? Limit)
    {
        public PlanFeatureResponse(PlanFeature p) : this(p.Id, p.PlanId, p.FeatureKey, p.IsEnabled, p.Value, p.Limit) { }
    }

    public record TenantOverrideResponse(Guid Id, string FeatureKey, bool IsEnabled, string? Value, int? Limit, string? Reason, DateTime? ExpiresAt)
    {
        public TenantOverrideResponse(TenantFeatureOverride o) : this(o.Id, o.FeatureKey, o.IsEnabled, o.Value, o.Limit, o.Reason, o.ExpiresAt) { }
    }

    public record FeatureCheckResponse(string FeatureKey, bool IsEnabled, string? Value, int? Limit, string Source);
}
