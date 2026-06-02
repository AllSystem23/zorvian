using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SupplierRepository : ISupplierRepository
{
    private readonly ZorvianDbContext _db;

    public SupplierRepository(ZorvianDbContext db)
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
