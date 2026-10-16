using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class EmploymentPeriodRepository : IEmploymentPeriodRepository
{
    private readonly ZorvianDbContext _db;

    public EmploymentPeriodRepository(ZorvianDbContext db) => _db = db;

    public async Task<EmploymentPeriod?> GetByIdAsync(Guid id) =>
        await _db.EmploymentPeriods
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<EmploymentPeriod>> GetByEmployeeAsync(Guid employeeId) =>
        await _db.EmploymentPeriods
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.PeriodNumber)
            .ToListAsync();

    public async Task<EmploymentPeriod?> GetOpenByEmployeeAsync(Guid employeeId) =>
        await _db.EmploymentPeriods
            .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.Status == "open");

    public async Task<int> GetNextPeriodNumberAsync(Guid employeeId) =>
        await _db.EmploymentPeriods
            .Where(p => p.EmployeeId == employeeId)
            .MaxAsync(p => (int?)p.PeriodNumber) + 1 ?? 1;

    public async Task AddAsync(EmploymentPeriod period) =>
        await _db.EmploymentPeriods.AddAsync(period);

    public Task UpdateAsync(EmploymentPeriod period)
    {
        _db.EmploymentPeriods.Update(period);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
