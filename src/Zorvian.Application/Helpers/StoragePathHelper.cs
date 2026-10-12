namespace Zorvian.Application.Helpers;

/// <summary>
/// Shared utility for extracting storage object paths from stored values.
/// A stored value can be a GCS URL (public or signed, optionally with query
/// string), a local rooted path (/path) or an already-normalized bare object
/// path (companies/{id}/logo.png). External URLs are not treated as storage.
/// </summary>
public static class StoragePathHelper
{
    private const string GcsHost = "storage.googleapis.com";

    public static string? ExtractStoragePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Has a URI scheme (absolute URL): only GCS objects are ours.
        if (value.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return null;

            // Exact host match: avoids firebasestorage.googleapis.com (a
            // different host) being caught by a naive substring check.
            if (!uri.Host.Equals(GcsHost, StringComparison.OrdinalIgnoreCase))
                return null;

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 1
                ? string.Join("/", segments.Skip(1)) // skip bucket name
                : null;
        }

        // No scheme: already-stored bare or rooted object path.
        var trimmed = value.TrimStart('/');
        return trimmed.Length > 0 ? trimmed : null;
    }
}
