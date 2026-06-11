using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Facade para cumplir con la Regla de los 3 Clics.
/// Orquesta la obtención de datos, renderizado y preparación de firma.
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Flujo simplificado: Genera y deja el documento listo para firma en un solo paso.
    /// 1. Obtiene datos de la entidad ERP.
    /// 2. Renderiza vía Liquid.
    /// 3. Crea el registro y lo pone en estado 'pending_signature'.
    /// </summary>
    Task<GeneratedDocument> QuickGenerateEmployeeContractAsync(Guid employeeId, Guid templateId);

    /// <summary>
    /// Genera y prepara un documento de venta/factura.
    /// </summary>
    Task<GeneratedDocument> QuickGenerateSaleDocumentAsync(Guid saleId, Guid templateId);
}
