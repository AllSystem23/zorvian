using Zorvian.Application.DTOs.Document;

namespace Zorvian.Application.Services.Documentary;

/// <summary>
/// Construye el contexto de datos de una entidad para el motor documental
/// (claves planas + anidadas + listas de ítems tabulados para {% for %}).
/// Extraído de DocumentGenerationService para no acumular dependencias de
/// repositorio en el servicio orquestador.
/// </summary>
public interface IEntityDocumentContextBuilder
{
    Task<EntityContextResponse?> BuildAsync(string entityType, Guid entityId);
}
