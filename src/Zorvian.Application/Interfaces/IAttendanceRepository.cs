using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAttendanceRepository
{
    Task<AttendanceRecord?> GetTodayRecordAsync(Guid employeeId);
    Task<AttendanceRecord?> GetByIdAsync(Guid id);
    Task<List<AttendanceRecord>> GetMonthlyAsync(Guid employeeId, int year, int month);
    Task AddAsync(AttendanceRecord record);
    Task UpdateAsync(AttendanceRecord record);
    Task SaveChangesAsync();
}
