using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SickLeaveRepository : ISickLeaveRepository
{
    private readonly ZorvianDbContext _db;

    public SickLeaveRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<SickLeaveRecord>> GetByEmployeeAsync(Guid employeeId) =>
        await _db.SickLeaveRecords
            .Where(sl => sl.EmployeeId == employeeId)
            .OrderByDescending(sl => sl.StartDate)
            .ToListAsync();

    public async Task<SickLeaveRecord?> GetByIdAsync(Guid id) =>
        await _db.SickLeaveRecords.FindAsync(id);

    public async Task AddAsync(SickLeaveRecord record) =>
        await _db.SickLeaveRecords.AddAsync(record);

    public Task UpdateAsync(SickLeaveRecord record)
    {
        _db.SickLeaveRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _db.SickLeaveRecords.FindAsync(id);
        if (entity != null) _db.SickLeaveRecords.Remove(entity);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
