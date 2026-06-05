using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class TerminationRepository : ITerminationRepository
{
    private readonly ZorvianDbContext _db;

    public TerminationRepository(ZorvianDbContext db) => _db = db;

    public async Task<TerminationRecord?> GetByIdAsync(Guid id) =>
        await _db.TerminationRecords
            .Include(tr => tr.Employee)
            .FirstOrDefaultAsync(tr => tr.Id == id);

    public async Task<List<TerminationRecord>> GetByEmployeeAsync(Guid employeeId) =>
        await _db.TerminationRecords
            .Where(tr => tr.EmployeeId == employeeId)
            .OrderByDescending(tr => tr.TerminationDate)
            .ToListAsync();

    public async Task AddAsync(TerminationRecord record) =>
        await _db.TerminationRecords.AddAsync(record);

    public Task UpdateAsync(TerminationRecord record)
    {
        _db.TerminationRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
