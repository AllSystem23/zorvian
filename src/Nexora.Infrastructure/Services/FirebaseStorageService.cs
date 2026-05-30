using FirebaseAdmin;
using Google.Cloud.Storage.V1;
using Nexora.Application.Interfaces;

namespace Nexora.Infrastructure.Services;

public sealed class FirebaseStorageService : IDocumentStorageService
{
    private readonly string _bucketName;
    private readonly StorageClient _storage;

    public FirebaseStorageService(string bucketName)
    {
        _bucketName = bucketName;
        var app = FirebaseApp.DefaultInstance
            ?? throw new InvalidOperationException("FirebaseApp not initialized");
        _storage = StorageClient.Create(app.Options.Credential);
    }

    public async Task<string> SaveFileAsync(string fileName, Stream content)
    {
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var obj = await _storage.UploadObjectAsync(_bucketName, $"uploads/{uniqueName}", null, content);
        return obj.Name;
    }

    public string GetFileUrl(string relativePath)
    {
        return $"https://storage.googleapis.com/{_bucketName}/{relativePath}";
    }
}
