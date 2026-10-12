using System.Net.Http;
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
}
