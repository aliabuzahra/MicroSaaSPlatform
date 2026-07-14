using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Storage.Service.Domain.Entities;

public class StoredFile : Entity<Guid>, IMustHaveTenant
{
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public Guid UploadedByUserId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StoragePath { get; set; }
    public long SizeBytes { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt != null;

    public static StoredFile Create(
        Guid tenantId,
        Guid uploadedByUserId,
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        string? category = null,
        string? description = null,
        bool isPublic = false)
    {
        return new StoredFile
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            UploadedByUserId = uploadedByUserId,
            FileName = fileName,
            ContentType = contentType,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            Category = category,
            Description = description,
            IsPublic = isPublic,
            UploadedAt = DateTime.UtcNow
        };
    }

    public void MarkAsDeleted()
    {
        DeletedAt = DateTime.UtcNow;
    }
}

public static class FileCategories
{
    public const string Avatar = "avatar";
    public const string Logo = "logo";
    public const string Document = "document";
    public const string Image = "image";
    public const string Attachment = "attachment";
    public const string Export = "export";
}
