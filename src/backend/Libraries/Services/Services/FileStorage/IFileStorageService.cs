using Microsoft.AspNetCore.Http;

namespace Domain.Services.FileStorage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string fileName);
    Task<(byte[], string)> GetFileAsync(string filePath);
    Task<string> GetFilePathAsync(string fileName);
    Task<bool> DeleteFileAsync(string filePath);
}
