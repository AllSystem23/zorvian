using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CostCenterRepository : ICostCenterRepository
{
    private readonly ZorvianDbContext _db;

    public CostCenterRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<CostCenter>> GetAllAsync(Guid companyId) =>
        await _db.Set<CostCenter>()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<CostCenter?> GetByIdAsync(Guid id) =>
        await _db.Set<CostCenter>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(CostCenter costCenter) =>
        await _db.Set<CostCenter>().AddAsync(costCenter);

    public Task UpdateAsync(CostCenter costCenter)
    {
        _db.Set<CostCenter>().Update(costCenter);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CostCenter costCenter)
    {
        _db.Set<CostCenter>().Remove(costCenter);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
