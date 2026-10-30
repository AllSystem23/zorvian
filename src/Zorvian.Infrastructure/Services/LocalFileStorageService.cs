using System.Text;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

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

        return path;
    }

    public Task DeleteFileAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public async Task<string?> ReadTextFileAsync(string path)
    {
        // Mismo modo relativo que UploadFileAsync (sin normalizar: el path lo
        // guardamos nosotros al subir).
        var relative = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        if (relative.Length == 0) return null;

        var fullPath = Path.Combine(_basePath,
            relative.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath)) return null;

        await using var stream = File.OpenRead(fullPath);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public Task<string?> GetSignedUrlAsync(string? storedPathOrUrl, TimeSpan? ttl = null)
    {
        var path = StoragePathHelper.ExtractStoragePath(storedPathOrUrl);
        if (string.IsNullOrEmpty(path))
            return Task.FromResult(storedPathOrUrl);

        // Local storage has no signing: serve as a rooted relative URL.
        return Task.FromResult<string?>($"/{path}");
    }

    public Task<IReadOnlyList<StorageObjectInfo>> ListFilesAsync(string prefix)
    {
        var results = new List<StorageObjectInfo>();

        var relativePrefix = (prefix ?? string.Empty)
            .Replace('\\', '/')
            .TrimStart('/');
        var directory = Path.Combine(_basePath,
            relativePrefix.Replace('/', Path.DirectorySeparatorChar));

        if (Directory.Exists(directory))
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(_basePath, file)
                    .Replace(Path.DirectorySeparatorChar, '/');
                results.Add(new StorageObjectInfo(relative, File.GetLastWriteTimeUtc(file)));
            }
        }

        return Task.FromResult<IReadOnlyList<StorageObjectInfo>>(results);
    }
}
