using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DocumentSignatureRepository : IDocumentSignatureRepository
{
    private readonly ZorvianDbContext _db;

    public DocumentSignatureRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<DocumentSignature>> GetSignaturesByDocumentIdAsync(Guid documentId) =>
        await _db.DocumentSignatures
            .Where(s => s.DocumentId == documentId)
            .ToListAsync();

    public async Task AddSignatureAsync(DocumentSignature signature) =>
        await _db.DocumentSignatures.AddAsync(signature);

    public Task UpdateSignatureAsync(DocumentSignature signature)
    {
        _db.DocumentSignatures.Update(signature);
        return Task.CompletedTask;
    }
}
