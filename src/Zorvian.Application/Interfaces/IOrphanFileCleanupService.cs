namespace Zorvian.Application.Interfaces;

/// <summary>
/// Extracts file URLs from entities marked for deletion and removes them from storage.
/// Works automatically for any entity with string properties ending in "Url", "Path", or "File".
/// </summary>
public interface IOrphanFileCleanupService
{
    /// <summary>
    /// Extracts all file URLs from the entity's string properties.
    /// </summary>
    IReadOnlyList<string> ExtractFileUrls(object entity);

    /// <summary>
    /// Deletes files from storage for the given URLs. Logs errors but never throws.
    /// </summary>
    Task CleanupFilesAsync(IEnumerable<string> urls, CancellationToken ct = default);
}
