using System.Net.Http;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class FirebaseStorageService : IDocumentStorageService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    private readonly string _bucketName;
    private readonly ILogger<FirebaseStorageService>? _logger;
    private StorageClient? _storage;
    private UrlSigner? _signer;

    public FirebaseStorageService(string bucketName, ILogger<FirebaseStorageService>? logger = null)
    {
        _bucketName = bucketName;
        _logger = logger;
    }

    private GoogleCredential GetCredential() =>
        FirebaseApp.DefaultInstance is { } app && app.Options.Credential is GoogleCredential cred
            ? cred
            : GoogleCredential.GetApplicationDefault();

    private StorageClient GetStorage() => _storage ??= StorageClient.Create(GetCredential());

    private UrlSigner GetSigner() => _signer ??= UrlSigner.FromCredential(GetCredential());

    public async Task<string> UploadFileAsync(Stream stream, string path, string contentType)
    {
        await GetStorage().UploadObjectAsync(_bucketName, path, contentType, stream);
        return path;
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

    public async Task<string?> ReadTextFileAsync(string path)
    {
        var objectPath = StoragePathHelper.ExtractStoragePath(path);
        if (string.IsNullOrEmpty(objectPath)) return null;

        try
        {
            using var buffer = new MemoryStream();
            await GetStorage().DownloadObjectAsync(_bucketName, objectPath, buffer);
            return Encoding.UTF8.GetString(buffer.ToArray());
        }
        catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
        {
            return null;
        }
    }

    public Task<string?> GetSignedUrlAsync(string? storedPathOrUrl, TimeSpan? ttl = null)
    {
        var path = StoragePathHelper.ExtractStoragePath(storedPathOrUrl);

        // Not a storage object (external URL) or empty: return as-is.
        if (string.IsNullOrEmpty(path))
            return Task.FromResult(storedPathOrUrl);

        try
        {
            var url = GetSigner().Sign(_bucketName, path, ttl ?? DefaultTtl, HttpMethod.Get);
            return Task.FromResult<string?>(url);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "No se pudo firmar la URL de {Path}; se usa la URL pública como fallback", path);
            return Task.FromResult<string?>($"https://storage.googleapis.com/{_bucketName}/{path}");
        }
    }

    public async Task<IReadOnlyList<StorageObjectInfo>> ListFilesAsync(string prefix)
    {
        var results = new List<StorageObjectInfo>();

        await foreach (var obj in GetStorage().ListObjectsAsync(_bucketName, prefix))
        {
            if (string.IsNullOrEmpty(obj.Name)) continue;

            // Si el SDK no informa la fecha se asume "recién subido": el barrido
            // prefiere no borrar antes que borrar un archivo en uso.
            var modified = obj.UpdatedDateTimeOffset
                           ?? obj.TimeCreatedDateTimeOffset
                           ?? DateTimeOffset.UtcNow;

            results.Add(new StorageObjectInfo(obj.Name, modified));
        }

        return results;
    }
}
