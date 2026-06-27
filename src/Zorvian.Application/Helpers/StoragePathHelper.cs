namespace Zorvian.Application.Helpers;

/// <summary>
/// Shared utility for extracting storage paths from file URLs.
/// Supports Firebase URLs (https://storage.googleapis.com/{bucket}/{path})
/// and local paths (/{path}).
/// </summary>
public static class StoragePathHelper
{
    public static string? ExtractStoragePath(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Firebase URL: https://storage.googleapis.com/{bucket}/{path}
        if (url.Contains("storage.googleapis.com", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 1)
                return string.Join("/", segments.Skip(1)); // skip bucket name
        }

        // Local path: /uploads/... or similar
        if (url.StartsWith('/') && url.Length > 1)
            return url.TrimStart('/');

        return null;
    }
}
