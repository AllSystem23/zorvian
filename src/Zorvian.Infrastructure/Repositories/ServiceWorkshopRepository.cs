using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ServiceWorkshopRepository : IServiceWorkshopRepository
{
    private readonly ZorvianDbContext _db;

    public ServiceWorkshopRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<ServiceWorkshop>> GetAllAsync(Guid companyId) =>
        await _db.Set<ServiceWorkshop>()
            .Include(w => w.Technicians)
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.Name)
            .ToListAsync();

    public async Task<ServiceWorkshop?> GetByIdAsync(Guid id) =>
        await _db.Set<ServiceWorkshop>()
            .Include(w => w.Technicians)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(ServiceWorkshop workshop) =>
        await _db.Set<ServiceWorkshop>().AddAsync(workshop);

    public Task UpdateAsync(ServiceWorkshop workshop)
    {
        _db.Set<ServiceWorkshop>().Update(workshop);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ServiceWorkshop workshop)
    {
        _db.Set<ServiceWorkshop>().Remove(workshop);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
