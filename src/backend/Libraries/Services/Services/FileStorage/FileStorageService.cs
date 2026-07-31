using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Domain.Services.FileStorage;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly string _fileStorageBasePath;

    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _fileStorageBasePath = _configuration["FileStorage:BasePath"]
            ?? throw new InvalidOperationException("FileStorage:BasePath configuration is required.");

        // Ensure the directory exists
        if (!Directory.Exists(_fileStorageBasePath))
            Directory.CreateDirectory(_fileStorageBasePath);
    }

    public async Task<string> SaveFileAsync(IFormFile file, string fileName)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided or file is empty");
        }

        // Create folder structure by year/month
        var currentDateTime = Shared.Helpers.DateTimeHelper.Now;
        var folderPath = Path.Combine(_fileStorageBasePath, currentDateTime.ToString("yyyy-MM"));

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // Generate a unique file name with original extension
        var fileExtension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(folderPath, uniqueFileName);

        // Save the file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(fileStream);

        // Return relative path for storage in database
        return Path.Combine(currentDateTime.ToString("yyyy-MM"), uniqueFileName);
    }

    public async Task<(byte[], string)> GetFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {filePath}");

        byte[] fileContents = await File.ReadAllBytesAsync(fullPath);
        string contentType = GetContentType(Path.GetExtension(fullPath));

        return (fileContents, contentType);
    }

    public Task<string> GetFilePathAsync(string fileName)
    {
        // Create folder structure by year/month
        var currentDateTime = Shared.Helpers.DateTimeHelper.Now;
        var relativePath = currentDateTime.ToString("yyyy-MM");
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        // Return relative path (to be used for future upload)
        return Task.FromResult(Path.Combine(relativePath, uniqueFileName));
    }

    private string GetContentType(string fileExtension)
    {
        switch (fileExtension.ToLower())
        {
            // Images
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            case ".gif": return "image/gif";
            case ".bmp": return "image/bmp";
            case ".webp": return "image/webp";
            case ".svg": return "image/svg+xml";
            case ".ico": return "image/x-icon";
            
            // Documents
            case ".pdf": return "application/pdf";
            case ".doc": return "application/msword";
            case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case ".xls": return "application/vnd.ms-excel";
            case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case ".ppt": return "application/vnd.ms-powerpoint";
            case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            
            // Text
            case ".txt": return "text/plain";
            case ".csv": return "text/csv";
            case ".json": return "application/json";
            case ".xml": return "application/xml";
            case ".html": return "text/html";
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            
            // Archives
            case ".zip": return "application/zip";
            case ".rar": return "application/x-rar-compressed";
            case ".7z": return "application/x-7z-compressed";
            case ".tar": return "application/x-tar";
            case ".gz": return "application/gzip";
            
            default: return "application/octet-stream";
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_fileStorageBasePath, filePath);

        return await Task.Run(() =>
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        });
    }
}
