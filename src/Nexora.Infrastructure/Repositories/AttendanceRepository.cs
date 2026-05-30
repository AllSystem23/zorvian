using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly NexoraDbContext _db;

    public AttendanceRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<AttendanceRecord?> GetTodayRecordAsync(Guid employeeId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);
    }

    public async Task<AttendanceRecord?> GetByIdAsync(Guid id)
    {
        return await _db.AttendanceRecords.FindAsync(id);
    }

    public async Task<List<AttendanceRecord>> GetMonthlyAsync(Guid employeeId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        return await _db.AttendanceRecords
            .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date <= end)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }

    public async Task AddAsync(AttendanceRecord record)
    {
        await _db.AttendanceRecords.AddAsync(record);
    }

    public Task UpdateAsync(AttendanceRecord record)
    {
        _db.AttendanceRecords.Update(record);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
