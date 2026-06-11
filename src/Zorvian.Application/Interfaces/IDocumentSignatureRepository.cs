using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDocumentSignatureRepository
{
    Task<List<DocumentSignature>> GetSignaturesByDocumentIdAsync(Guid documentId);
    Task AddSignatureAsync(DocumentSignature signature);
    Task UpdateSignatureAsync(DocumentSignature signature);
}
