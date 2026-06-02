using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByCodeAsync(string code, Guid branchId);
    Task<List<Client>> GetFilteredAsync(string? search, string? status, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, Guid branchId);
    Task<string> GenerateCodeAsync(Guid companyId);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Client client);
    Task SaveChangesAsync();
}
