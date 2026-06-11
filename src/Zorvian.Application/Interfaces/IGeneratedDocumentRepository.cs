using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IGeneratedDocumentRepository
{
    Task<GeneratedDocument?> GetByIdAsync(Guid id);
    Task<List<GeneratedDocument>> GetByEntityAsync(Guid entityId, string entityType);
    Task<List<GeneratedDocument>> GetPendingApprovalAsync();
    Task AddAsync(GeneratedDocument document);
    Task UpdateAsync(GeneratedDocument document);
    Task SaveChangesAsync();

    // Versioning
    Task AddVersionAsync(DocumentVersion version);
    Task<List<DocumentVersion>> GetVersionsAsync(Guid documentId);

    // Signatures
    Task AddSignatureAsync(DocumentSignature signature);
    Task<List<DocumentSignature>> GetSignaturesAsync(Guid documentId);
}
