namespace Domain.Services.FileStorage;

/// <summary>
/// Unified file storage provider interface.
/// Supports both local disk and S3-compatible cloud storage.
/// </summary>
public interface IFileStorageProvider
{
    Task<string> UploadAsync(string relativePath, Stream content, string contentType);
    Task<Stream> DownloadAsync(string relativePath);
    Task DeleteAsync(string relativePath);
    Task<string?> GetSignedUrlAsync(string relativePath, TimeSpan? expiry = null);
    Task<bool> ExistsAsync(string relativePath);
}
