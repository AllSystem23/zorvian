using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.Documentary;

/// <summary>
/// Resuelve el HTML renderizado de una versión: de la BD si aún lo guarda
/// (fallback de subida fallida o filas antiguas) o descargándolo de Storage
/// (modelo normal). Cualquier fallo de Storage se loguea y devuelve null para
/// que un problema de descarga no tumbe la petición (el endpoint responde 404
/// con mensaje claro en lugar de un 500).
/// </summary>
public sealed class DocumentVersionContentService : IDocumentVersionContentService
{
    private readonly IDocumentStorageService _storage;
    private readonly ILogger<DocumentVersionContentService> _logger;

    public DocumentVersionContentService(
        IDocumentStorageService storage,
        ILogger<DocumentVersionContentService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<string?> GetContentAsync(DocumentVersion version)
    {
        if (version is null) return null;

        // 1) Todavía en la BD: filas antiguas o fallback de subida fallida.
        if (!string.IsNullOrEmpty(version.Content))
            return version.Content;

        // 2) Modelo nuevo: el HTML vive en Storage.
        var path = version.FilePath;
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            return await _storage.ReadTextFileAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo leer el contenido de Storage para la versión {VersionId} ({Path})",
                version.Id, path);
            return null;
        }
    }
}
