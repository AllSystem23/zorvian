using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly NexoraDbContext _db;

    public ClientRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<Client?> GetByIdAsync(Guid id) =>
        await _db.Set<Client>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Client?> GetByCodeAsync(string code, Guid branchId) =>
        await _db.Set<Client>().FirstOrDefaultAsync(c => c.Code == code && c.BranchId == branchId);

    public async Task<List<Client>> GetFilteredAsync(string? search, string? status, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Client>().Where(c => c.BranchId == branchId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(s) ||
                c.LastName.ToLower().Contains(s) ||
                (c.IdentificationNumber != null && c.IdentificationNumber.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        return await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? search, string? status, Guid branchId)
    {
        var query = _db.Set<Client>().Where(c => c.BranchId == branchId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(s) ||
                c.LastName.ToLower().Contains(s) ||
                (c.IdentificationNumber != null && c.IdentificationNumber.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        return await query.CountAsync();
    }

    public async Task<string> GenerateCodeAsync(Guid companyId)
    {
        var count = await _db.Set<Client>().CountAsync(c => c.CompanyId == companyId);
        return $"CLI-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task AddAsync(Client client) =>
        await _db.Set<Client>().AddAsync(client);

    public Task UpdateAsync(Client client)
    {
        _db.Set<Client>().Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Client client)
    {
        _db.Set<Client>().Remove(client);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
