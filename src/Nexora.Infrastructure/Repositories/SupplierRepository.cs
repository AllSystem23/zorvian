using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class SupplierRepository : ISupplierRepository
{
    private readonly NexoraDbContext _db;

    public SupplierRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<List<Supplier>> GetAllAsync(Guid companyId) =>
        await _db.Set<Supplier>()
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<Supplier?> GetByIdAsync(Guid id) =>
        await _db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(Supplier supplier) =>
        await _db.Set<Supplier>().AddAsync(supplier);

    public Task UpdateAsync(Supplier supplier)
    {
        _db.Set<Supplier>().Update(supplier);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Supplier supplier)
    {
        _db.Set<Supplier>().Remove(supplier);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
