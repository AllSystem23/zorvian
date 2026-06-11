using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDocumentService
{
    /// <summary>
    /// Sugiere plantillas de documentos basadas en el contexto del negocio (módulo, evento, país).
    /// </summary>
    Task<List<DocumentTemplate>> SuggestTemplatesAsync(string module, string @event, string countryCode);

    /// <summary>
    /// Genera un documento altamente profesional usando el motor de plantillas Liquid (Fluid).
    /// </summary>
    /// <param name="templateId">ID de la plantilla base.</param>
    /// <param name="entityId">ID del registro relacionado (Empleado, Venta, etc).</param>
    /// <param name="variableData">Objeto con los datos para inyectar en la plantilla (Model).</param>
    /// <returns>El documento generado con su primera versión.</returns>
    Task<GeneratedDocument> GenerateProfessionalDocumentAsync(Guid templateId, Guid entityId, object variableData);

    /// <summary>
    /// Procesa el ciclo de vida del documento en un solo paso (Aprobar y Preparar Firma).
    /// </summary>
    Task FinalizeAndRequestSignatureAsync(Guid documentId, string signerRole, Guid signerId);

    /// <summary>
    /// Obtiene el historial completo de un documento, incluyendo versiones y firmas.
    /// </summary>
    Task<GeneratedDocument?> GetDocumentDetailsAsync(Guid documentId);
}
