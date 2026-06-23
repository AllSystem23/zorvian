using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class GeneratedDocumentRepository : IGeneratedDocumentRepository
{
    private readonly ZorvianDbContext _db;

    public GeneratedDocumentRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<GeneratedDocument?> GetByIdAsync(Guid id) =>
        await _db.GeneratedDocuments
            .Include(d => d.Template)
            .Include(d => d.Versions)
            .Include(d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<GeneratedDocument>> GetByEntityAsync(Guid entityId, string entityType) =>
        await _db.GeneratedDocuments
            .Where(d => d.EntityId == entityId && d.EntityType == entityType)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<List<GeneratedDocument>> GetPendingApprovalAsync() =>
        await _db.GeneratedDocuments
            .Where(d => d.Status == "pending")
            .Include(d => d.Template)
            .ToListAsync();

    public async Task<(List<GeneratedDocument> Items, int Total)> GetAllPagedAsync(int page, int pageSize)
    {
        var query = _db.GeneratedDocuments
            .Include(d => d.Template)
            .OrderByDescending(d => d.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(GeneratedDocument document) =>
        await _db.GeneratedDocuments.AddAsync(document);

    public Task UpdateAsync(GeneratedDocument document)
    {
        _db.GeneratedDocuments.Update(document);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

    // Versioning
    public async Task AddVersionAsync(DocumentVersion version) =>
        await _db.DocumentVersions.AddAsync(version);

    public async Task<List<DocumentVersion>> GetVersionsAsync(Guid documentId) =>
        await _db.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

    // Signatures
    public async Task AddSignatureAsync(DocumentSignature signature) =>
        await _db.DocumentSignatures.AddAsync(signature);

    public async Task<List<DocumentSignature>> GetSignaturesAsync(Guid documentId) =>
        await _db.DocumentSignatures
            .Where(s => s.DocumentId == documentId)
            .ToListAsync();
}
