namespace SaaS.Storage.Service.Infrastructure.Storage;

public interface IStorageProvider
{
    Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken cancellationToken = default);
}

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration configuration, ILogger<LocalStorageProvider> logger)
    {
        _basePath = configuration["Storage:LocalPath"] ?? "/tmp/saas-storage";
        _logger = logger;
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(Guid tenantId, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var tenantPath = Path.Combine(_basePath, tenantId.ToString());
        if (!Directory.Exists(tenantPath))
        {
            Directory.CreateDirectory(tenantPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(tenantPath, uniqueFileName);
        var storagePath = $"{tenantId}/{uniqueFileName}";

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("File uploaded: {StoragePath}", storagePath);
        return storagePath;
    }

    public async Task<Stream?> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storagePath);
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {StoragePath}", storagePath);
            return null;
        }

        var memoryStream = new MemoryStream();
        await using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        
        return memoryStream;
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storagePath);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted: {StoragePath}", storagePath);
        }

        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(string storagePath, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/api/storage/files/download/{Uri.EscapeDataString(storagePath)}");
    }
}
