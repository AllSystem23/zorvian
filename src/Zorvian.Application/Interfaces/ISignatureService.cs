using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISignatureService
{
    Task<DocumentSignature> SignDocumentAsync(Guid documentId, string signerRole, string? notes);
    Task<List<DocumentSignature>> GetSignaturesByDocumentAsync(Guid documentId);
}
