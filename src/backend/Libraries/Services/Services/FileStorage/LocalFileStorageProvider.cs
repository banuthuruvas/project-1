using Microsoft.Extensions.Logging;

namespace Domain.Services.FileStorage;

/// <summary>
/// Local disk file storage provider.
/// Uses the configured FileStorage:BasePath for all operations.
/// </summary>
public class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    public LocalFileStorageProvider(string basePath, ILogger<LocalFileStorageProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;
    }

    public Task<string> UploadAsync(string relativePath, Stream content, string contentType)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        content.CopyTo(fileStream);

        _logger.LogInformation("File uploaded locally: {Path}", relativePath);
        return Task.FromResult(relativePath);
    }

    public Task<Stream> DownloadAsync(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", fullPath);

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted locally: {Path}", relativePath);
        }
        return Task.CompletedTask;
    }

    public Task<string?> GetSignedUrlAsync(string relativePath, TimeSpan? expiry = null)
    {
        // Local storage doesn't support signed URLs — return null
        return Task.FromResult<string?>(null);
    }

    public Task<bool> ExistsAsync(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }
}
