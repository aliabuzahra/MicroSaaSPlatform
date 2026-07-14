using Microsoft.EntityFrameworkCore;
using SaaS.ApiKeys.Service.Domain.Entities;
using SaaS.ApiKeys.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;

namespace SaaS.ApiKeys.Service.Features.ApiKeys;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/keys").RequireAuthorization();

        group.MapPost("/", async (
            CreateApiKeyRequest request,
            HttpContext context,
            ApiKeysDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            var userId = context.GetCurrentUserId();

            if (!tenantId.HasValue || !userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var invalidScopes = request.Scopes?.Where(s => !ApiKeyScopes.IsValid(s)).ToList();
            if (invalidScopes?.Any() == true)
            {
                return Results.BadRequest(new { Error = $"Invalid scopes: {string.Join(", ", invalidScopes)}" });
            }

            var (apiKey, rawKey) = ApiKey.Create(
                request.Name,
                tenantId.Value,
                userId.Value,
                request.Scopes,
                request.ExpiresAt);

            db.ApiKeys.Add(apiKey);
            await db.SaveChangesAsync();

            return Results.Created($"/api/keys/{apiKey.Id}", new CreateApiKeyResponse(
                apiKey.Id,
                apiKey.Name,
                rawKey,
                apiKey.KeyPrefix,
                apiKey.Scopes,
                apiKey.ExpiresAt,
                apiKey.CreatedAt));
        });

        group.MapGet("/", async (HttpContext context, ApiKeysDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return Results.Unauthorized();
            }

            var keys = await db.ApiKeys
                .Where(k => k.TenantId == new SaaS.Shared.Kernel.BuildingBlocks.TenantId(tenantId.Value))
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new ApiKeyResponse(
                    k.Id,
                    k.Name,
                    k.KeyPrefix,
                    k.Scopes,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.LastUsedAt,
                    k.RevokedAt != null ? "Revoked" : k.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active"))
                .ToListAsync();

            return Results.Ok(keys);
        });

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, ApiKeysDbContext db) =>
        {
            var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id);
            if (key is null)
            {
                return Results.NotFound(new { Error = "API key not found." });
            }

            return Results.Ok(new ApiKeyResponse(
                key.Id,
                key.Name,
                key.KeyPrefix,
                key.Scopes,
                key.CreatedAt,
                key.ExpiresAt,
                key.LastUsedAt,
                key.RevokedAt != null ? "Revoked" : key.ExpiresAt < DateTime.UtcNow ? "Expired" : "Active"));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            RevokeApiKeyRequest? request,
            HttpContext context,
            ApiKeysDbContext db) =>
        {
            var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id);
            if (key is null)
            {
                return Results.NotFound(new { Error = "API key not found." });
            }

            if (key.RevokedAt != null)
            {
                return Results.BadRequest(new { Error = "API key is already revoked." });
            }

            key.Revoke(request?.Reason);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "API key revoked successfully." });
        });

        app.MapPost("/api/keys/validate", async (
            ValidateApiKeyRequest request,
            ApiKeysDbContext db) =>
        {
            var keyPrefix = request.ApiKey.Length >= 8 ? request.ApiKey[..8] : request.ApiKey;
            
            var keys = await db.ApiKeys
                .IgnoreQueryFilters()
                .Where(k => k.KeyPrefix == keyPrefix)
                .ToListAsync();

            var matchingKey = keys.FirstOrDefault(k => k.ValidateKey(request.ApiKey));
            
            if (matchingKey is null)
            {
                return Results.Ok(new ValidateApiKeyResponse(false, null, null, null, null));
            }

            matchingKey.UpdateLastUsed();
            await db.SaveChangesAsync();

            return Results.Ok(new ValidateApiKeyResponse(
                true,
                matchingKey.Id,
                matchingKey.TenantId.Value,
                matchingKey.Name,
                matchingKey.Scopes));
        });
    }

    public record CreateApiKeyRequest(string Name, List<string>? Scopes, DateTime? ExpiresAt);
    public record RevokeApiKeyRequest(string? Reason);
    public record ValidateApiKeyRequest(string ApiKey);

    public record CreateApiKeyResponse(
        Guid Id,
        string Name,
        string ApiKey,
        string KeyPrefix,
        List<string> Scopes,
        DateTime? ExpiresAt,
        DateTime CreatedAt);

    public record ApiKeyResponse(
        Guid Id,
        string Name,
        string KeyPrefix,
        List<string> Scopes,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? LastUsedAt,
        string Status);

    public record ValidateApiKeyResponse(
        bool IsValid,
        Guid? ApiKeyId,
        Guid? TenantId,
        string? KeyName,
        List<string>? Scopes);
}
