using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IGeneratedDocumentRepository
{
    Task<GeneratedDocument?> GetByIdAsync(Guid id);
    /// <summary>
    /// Lectura SIN query filter de compañía, solo para el endpoint público de
    /// verificación (<c>[AllowAnonymous]</c>): sin tenant resuelto el filtro
    /// normal devolvería null y el QR daría 404. Sigue excluyendo los borrados
    /// lógicos. Solo expone nombre/estado/huellas, no el contenido.
    /// </summary>
    Task<GeneratedDocument?> GetByIdForPublicVerificationAsync(Guid id);
    Task<List<GeneratedDocument>> GetByEntityAsync(Guid entityId, string entityType);
    Task<List<GeneratedDocument>> GetPendingApprovalAsync();
    Task<(List<GeneratedDocument> Items, int Total)> GetAllPagedAsync(int page, int pageSize);
    Task AddAsync(GeneratedDocument document);
    Task UpdateAsync(GeneratedDocument document);
    /// <summary>
    /// Borrado FÍSICO del documento junto con sus versiones y firmas. Lo usa el
    /// endpoint de eliminación permanente; el borrado lógico normal es
    /// <see cref="UpdateAsync"/> con <c>IsDeleted = true</c>.
    /// </summary>
    Task DeleteAsync(GeneratedDocument document);
    Task SaveChangesAsync();

    // Versioning
    Task AddVersionAsync(DocumentVersion version);
    Task<List<DocumentVersion>> GetVersionsAsync(Guid documentId);

    // Signatures
    Task AddSignatureAsync(DocumentSignature signature);
    Task<List<DocumentSignature>> GetSignaturesAsync(Guid documentId);
}
