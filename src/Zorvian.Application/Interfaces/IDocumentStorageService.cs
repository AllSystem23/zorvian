namespace Zorvian.Application.Interfaces;

/// <summary>
/// Objeto almacenado (solo metadatos): ruta interna y última modificación.
/// Lo usa el barrido de huérfanos para no borrar subidas en curso.
/// </summary>
public sealed record StorageObjectInfo(string Path, DateTimeOffset LastModifiedUtc);

public interface IDocumentStorageService
{
    /// <summary>
    /// Uploads a file and returns the stored object path (e.g.
    /// "companies/{id}/logo.png"), NOT a URL. The path is what should be
    /// persisted on the entity; a signed URL is generated on read via
    /// <see cref="GetSignedUrlAsync"/>.
    /// </summary>
    Task<string> UploadFileAsync(Stream stream, string path, string contentType);

    Task DeleteFileAsync(string path);

    /// <summary>
    /// Resolves a stored value (object path, legacy public URL or external URL)
    /// into a time-limited signed URL. Non-storage external URLs are returned
    /// unchanged. Returns null when the input is null/empty.
    /// </summary>
    Task<string?> GetSignedUrlAsync(string? storedPathOrUrl, TimeSpan? ttl = null);

    /// <summary>
    /// Lists every stored object under <paramref name="prefix"/> (e.g.
    /// "companies/"). Used by <c>CompanyLogoOrphanSweepJob</c> to find files no
    /// row references. Returns an empty list when the prefix has no objects.
    /// </summary>
    Task<IReadOnlyList<StorageObjectInfo>> ListFilesAsync(string prefix);
}
