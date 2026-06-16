using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IDocumentTypeRepository
{
    Task<List<DocumentType>> GetAllAsync();
    Task<List<DocumentType>> GetByEntityTypeAsync(string entityType);
    Task<DocumentType?> GetByIdAsync(Guid id);
    Task AddAsync(DocumentType documentType);
    Task UpdateAsync(DocumentType documentType);
    Task DeleteAsync(DocumentType documentType);
    Task SaveChangesAsync();
}
