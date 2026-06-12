using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id);
    Task<List<Lead>> GetFilteredAsync(string? search, string? status, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, Guid companyId);
    Task AddAsync(Lead lead);
    Task UpdateAsync(Lead lead);
    Task DeleteAsync(Lead lead);
    Task SaveChangesAsync();
}
