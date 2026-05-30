using Nexora.Application.Interfaces;

namespace Nexora.Infrastructure.Services;

public sealed class LocalFileStorageService : IDocumentStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string basePath)
    {
        _basePath = basePath;
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadFileAsync(Stream stream, string path, string contentType)
    {
        var fullPath = Path.Combine(_basePath, path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return GetFileUrl(path);
    }

    public Task DeleteFileAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        
        return Task.CompletedTask;
    }

    public string GetFileUrl(string path)
    {
        return $"/{path.Replace("\\", "/")}";
    }
}
