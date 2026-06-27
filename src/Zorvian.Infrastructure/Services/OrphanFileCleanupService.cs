using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Extracts file URLs from entity properties using reflection and deletes them from storage.
/// Matches properties ending in Url, Path, or File (string type) on BaseEntity-derived entities.
/// </summary>
public sealed class OrphanFileCleanupService : IOrphanFileCleanupService
{
    private readonly IDocumentStorageService _storage;
    private readonly ILogger<OrphanFileCleanupService> _logger;

    private static readonly Regex FileSuffixPattern = new(
        @"(Url|Path|File|Document)$",
        RegexOptions.Compiled);

    public OrphanFileCleanupService(
        IDocumentStorageService storage,
        ILogger<OrphanFileCleanupService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public IReadOnlyList<string> ExtractFileUrls(object entity)
    {
        var urls = new List<string>();
        var type = entity.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(string)) continue;
            if (!FileSuffixPattern.IsMatch(prop.Name)) continue;
            if (prop.Name == nameof(BaseEntity.TenantId)) continue;

            var value = prop.GetValue(entity) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;

            if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
            {
                urls.Add(value);
            }
        }

        return urls;
    }

    public async Task CleanupFilesAsync(IEnumerable<string> urls, CancellationToken ct = default)
    {
        foreach (var url in urls)
        {
            try
            {
                var path = StoragePathHelper.ExtractStoragePath(url);
                if (!string.IsNullOrEmpty(path))
                {
                    await _storage.DeleteFileAsync(path);
                    _logger.LogInformation("Cleaned up orphan file: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up orphan file: {Url}", url);
            }
        }
    }
}
