using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly ZorvianDbContext _db;

    public DocumentTypeRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<DocumentType>> GetAllAsync() =>
        await _db.Set<DocumentType>()
            .OrderBy(d => d.Name)
            .ToListAsync();

    public async Task<List<DocumentType>> GetByEntityTypeAsync(string entityType) =>
        await _db.Set<DocumentType>()
            .Where(d => d.EntityType == entityType && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

    public async Task<DocumentType?> GetByIdAsync(Guid id) =>
        await _db.Set<DocumentType>().FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(DocumentType documentType) =>
        await _db.Set<DocumentType>().AddAsync(documentType);

    public Task UpdateAsync(DocumentType documentType)
    {
        _db.Set<DocumentType>().Update(documentType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DocumentType documentType)
    {
        _db.Set<DocumentType>().Remove(documentType);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
