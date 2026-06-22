using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDocumentTemplateRepository
{
    Task<DocumentTemplate?> GetByIdAsync(Guid id);
    Task<List<DocumentTemplate>> GetAllAsync();
    Task<List<DocumentTemplate>> GetByCategoryAsync(string category);
    Task<List<DocumentTemplate>> GetByCountryAsync(string countryCode);
    Task<(List<DocumentTemplate> Items, int Total)> GetFilteredAsync(
        string? category, string? countryCode, string? search, string? module,
        bool? isActive, int page, int pageSize);
    Task AddAsync(DocumentTemplate template);
    Task UpdateAsync(DocumentTemplate template);
    Task DeleteAsync(DocumentTemplate template);
    Task SaveChangesAsync();
}
