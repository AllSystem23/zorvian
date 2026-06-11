using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SignatureService : ISignatureService
{
    private readonly IDocumentSignatureRepository _sigRepo;
    private readonly IGeneratedDocumentRepository _docRepo;

    public SignatureService(IDocumentSignatureRepository sigRepo, IGeneratedDocumentRepository docRepo)
    {
        _sigRepo = sigRepo;
        _docRepo = docRepo;
    }

    public async Task<DocumentSignature> SignDocumentAsync(Guid documentId, string signerRole, string? notes)
    {
        var signature = new DocumentSignature
        {
            DocumentId = documentId,
            SignerRole = signerRole,
            Status = "signed",
            SignedAt = DateTime.UtcNow,
            Notes = notes
        };

        await _sigRepo.AddSignatureAsync(signature);
        
        // Update document status if necessary
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc != null)
        {
            doc.Status = "signed";
            await _docRepo.UpdateAsync(doc);
        }

        return signature;
    }

    public async Task<List<DocumentSignature>> GetSignaturesByDocumentAsync(Guid documentId)
    {
        return await _sigRepo.GetSignaturesByDocumentIdAsync(documentId);
    }
}
