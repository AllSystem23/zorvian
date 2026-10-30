using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Único punto de acceso al HTML renderizado de una <see cref="DocumentVersion"/>.
/// El contenido vive en Storage (<c>documents/{docId}/vN.html</c>) para no llenar la
/// base de datos (~40-80 KB por versión); la columna <c>Content</c> queda como
/// fallback y para las filas migradas antes del cambio. Todos los lectores (PDF,
/// verificación QR, constancia, resumen IA) pasan por aquí.
/// </summary>
public interface IDocumentVersionContentService
{
    /// <summary>Devuelve el HTML de la versión, o null si no existe en ningún sitio.</summary>
    Task<string?> GetContentAsync(DocumentVersion version);
}
