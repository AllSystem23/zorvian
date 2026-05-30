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

    public async Task<string> SaveFileAsync(string fileName, Stream content)
    {
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(_basePath, uniqueName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(stream);

        return Path.Combine("uploads", uniqueName);
    }

    public string GetFileUrl(string relativePath)
    {
        return $"/{relativePath.Replace("\\", "/")}";
    }
}
