using FirebaseAdmin;
using Google.Cloud.Storage.V1;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class FirebaseStorageService : IDocumentStorageService
{
    private readonly string _bucketName;
    private readonly StorageClient _storage;

    public FirebaseStorageService(string bucketName)
    {
        _bucketName = bucketName;
        // DefaultInstance might be null if not initialized in Program.cs
        var app = FirebaseApp.DefaultInstance;
        if (app != null)
        {
            _storage = StorageClient.Create(app.Options.Credential);
        }
        else
        {
            // Fallback for local development if credentials are provided via env or ADC
            _storage = StorageClient.Create();
        }
    }

    public async Task<string> UploadFileAsync(Stream stream, string path, string contentType)
    {
        await _storage.UploadObjectAsync(_bucketName, path, contentType, stream);
        return GetFileUrl(path);
    }

    public async Task DeleteFileAsync(string path)
    {
        try
        {
            await _storage.DeleteObjectAsync(_bucketName, path);
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            // Object already deleted or never existed
        }
    }

    public string GetFileUrl(string path)
    {
        return $"https://storage.googleapis.com/{_bucketName}/{path}";
    }
}
