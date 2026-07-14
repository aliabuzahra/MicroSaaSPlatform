using Microsoft.EntityFrameworkCore;
using SaaS.Storage.Service.Domain.Entities;
using SaaS.Storage.Service.Infrastructure.Persistence;
using SaaS.Storage.Service.Infrastructure.Storage;
using SaaS.Shared.Kernel.Authorization;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Storage.Service.Features.Files;

public static class FileEndpoints
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/storage/files").RequireAuthorization();

        group.MapPost("/upload", async (
            HttpContext context,
            IFormFile file,
            string? category,
            string? description,
            bool? isPublic,
            StorageDbContext db,
            IStorageProvider storageProvider) =>
        {
            var tenantId = context.GetCurrentTenantId();
            var userId = context.GetCurrentUserId();

            if (!tenantId.HasValue || !userId.HasValue)
                return Results.Unauthorized();

            if (file.Length == 0)
                return Results.BadRequest(new { Error = "File is empty." });

            if (file.Length > MaxFileSizeBytes)
                return Results.BadRequest(new { Error = $"File exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024}MB." });

            await using var stream = file.OpenReadStream();
            var storagePath = await storageProvider.UploadAsync(
                tenantId.Value,
                file.FileName,
                stream,
                file.ContentType);

            var storedFile = StoredFile.Create(
                tenantId.Value,
                userId.Value,
                file.FileName,
                file.ContentType,
                storagePath,
                file.Length,
                category,
                description,
                isPublic ?? false);

            db.Files.Add(storedFile);
            await db.SaveChangesAsync();

            return Results.Created($"/api/storage/files/{storedFile.Id}", new FileResponse(storedFile));
        }).DisableAntiforgery();

        group.MapGet("/", async (
            HttpContext context,
            StorageDbContext db,
            string? category,
            int? page,
            int? pageSize) =>
        {
            var query = db.Files.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(f => f.Category == category);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 20, 100);

            var files = await query
                .OrderByDescending(f => f.UploadedAt)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(f => new FileResponse(f))
                .ToListAsync();

            return Results.Ok(new { Items = files, TotalCount = totalCount, Page = actualPage, PageSize = actualPageSize });
        });

        group.MapGet("/{id:guid}", async (Guid id, StorageDbContext db) =>
        {
            var file = await db.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file is null)
                return Results.NotFound(new { Error = "File not found." });

            return Results.Ok(new FileResponse(file));
        });

        group.MapGet("/{id:guid}/download", async (
            Guid id,
            StorageDbContext db,
            IStorageProvider storageProvider) =>
        {
            var file = await db.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file is null)
                return Results.NotFound(new { Error = "File not found." });

            var stream = await storageProvider.DownloadAsync(file.StoragePath);
            if (stream is null)
                return Results.NotFound(new { Error = "File content not found." });

            return Results.File(stream, file.ContentType, file.FileName);
        });

        group.MapGet("/{id:guid}/url", async (
            Guid id,
            int? expiryMinutes,
            StorageDbContext db,
            IStorageProvider storageProvider) =>
        {
            var file = await db.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file is null)
                return Results.NotFound(new { Error = "File not found." });

            var expiry = TimeSpan.FromMinutes(expiryMinutes ?? 60);
            var url = await storageProvider.GetPresignedUrlAsync(file.StoragePath, expiry);

            return Results.Ok(new { Url = url, ExpiresIn = expiry.TotalSeconds });
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            StorageDbContext db,
            IStorageProvider storageProvider) =>
        {
            var file = await db.Files.FirstOrDefaultAsync(f => f.Id == id);
            if (file is null)
                return Results.NotFound(new { Error = "File not found." });

            await storageProvider.DeleteAsync(file.StoragePath);
            file.MarkAsDeleted();
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "File deleted." });
        });

        group.MapGet("/usage", async (HttpContext context, StorageDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Results.Unauthorized();

            var stats = await db.Files
                .Where(f => f.TenantId == new TenantId(tenantId.Value))
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFiles = g.Count(),
                    TotalSizeBytes = g.Sum(f => f.SizeBytes)
                })
                .FirstOrDefaultAsync();

            return Results.Ok(new
            {
                TotalFiles = stats?.TotalFiles ?? 0,
                TotalSizeBytes = stats?.TotalSizeBytes ?? 0,
                TotalSizeMB = (stats?.TotalSizeBytes ?? 0) / 1024.0 / 1024.0
            });
        });
    }

    public record FileResponse(
        Guid Id,
        string FileName,
        string ContentType,
        long SizeBytes,
        string? Category,
        string? Description,
        bool IsPublic,
        DateTime UploadedAt)
    {
        public FileResponse(StoredFile f) : this(
            f.Id, f.FileName, f.ContentType, f.SizeBytes, f.Category, f.Description, f.IsPublic, f.UploadedAt)
        { }
    }
}
