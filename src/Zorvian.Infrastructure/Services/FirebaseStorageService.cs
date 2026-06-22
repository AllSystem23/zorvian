using FirebaseAdmin;
using Google.Cloud.Storage.V1;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class FirebaseStorageService : IDocumentStorageService
{
    private readonly string _bucketName;
    private StorageClient? _storage;

    public FirebaseStorageService(string bucketName)
    {
        _bucketName = bucketName;
    }

    private StorageClient GetStorage()
    {
        _storage ??= FirebaseApp.DefaultInstance is { } app
            ? StorageClient.Create(app.Options.Credential)
            : StorageClient.Create();
        return _storage;
    }

    public async Task<string> UploadFileAsync(Stream stream, string path, string contentType)
    {
        await GetStorage().UploadObjectAsync(_bucketName, path, contentType, stream);
        return GetFileUrl(path);
    }

    public async Task DeleteFileAsync(string path)
    {
        try
        {
            await GetStorage().DeleteObjectAsync(_bucketName, path);
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
