using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class WorkOrderRepository : IWorkOrderRepository
{
    private readonly ZorvianDbContext _db;

    public WorkOrderRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WorkOrder>> GetAllAsync(Guid companyId) =>
        await _db.Set<WorkOrder>()
            .Include(w => w.Vehicle)
            .Include(w => w.Driver)
            .Include(w => w.FailureType)
            .Include(w => w.Workshop)
            .Where(w => w.TenantId == companyId.ToString())
            .OrderByDescending(w => w.ReportDateTime)
            .ToListAsync();

    public async Task<WorkOrder?> GetByIdAsync(Guid id) =>
        await _db.Set<WorkOrder>()
            .Include(w => w.Vehicle)
            .Include(w => w.Driver)
            .Include(w => w.FailureType)
            .Include(w => w.Workshop)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(WorkOrder workOrder) =>
        await _db.Set<WorkOrder>().AddAsync(workOrder);

    public Task UpdateAsync(WorkOrder workOrder)
    {
        _db.Set<WorkOrder>().Update(workOrder);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WorkOrder workOrder)
    {
        _db.Set<WorkOrder>().Remove(workOrder);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
