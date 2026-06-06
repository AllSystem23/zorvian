using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyProviderRepository : IWarrantyProviderRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyProviderRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<WarrantyProvider>> GetAllAsync(Guid companyId) =>
        await _db.Set<WarrantyProvider>()
            .Include(p => p.Contacts)
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<WarrantyProvider?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantyProvider>()
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(WarrantyProvider provider) =>
        await _db.Set<WarrantyProvider>().AddAsync(provider);

    public Task UpdateAsync(WarrantyProvider provider)
    {
        _db.Set<WarrantyProvider>().Update(provider);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WarrantyProvider provider)
    {
        _db.Set<WarrantyProvider>().Remove(provider);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
