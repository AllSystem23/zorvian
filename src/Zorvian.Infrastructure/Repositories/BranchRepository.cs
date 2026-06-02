using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BranchRepository : IBranchRepository
{
    private readonly ZorvianDbContext _db;

    public BranchRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<Branch>> GetAllAsync(Guid companyId) =>
        await _db.Set<Branch>()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .ToListAsync();

    public async Task<Branch?> GetByIdAsync(Guid id) =>
        await _db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(Branch branch) =>
        await _db.Set<Branch>().AddAsync(branch);

    public Task UpdateAsync(Branch branch)
    {
        _db.Set<Branch>().Update(branch);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Branch branch)
    {
        _db.Set<Branch>().Remove(branch);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
