using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPermissionRepository
{
    Task<PermissionRequest?> GetByIdAsync(Guid id);
    Task<List<PermissionRequest>> GetFilteredAsync(string? status, Guid? employeeId, Guid? leaveTypeId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? status, Guid? employeeId, Guid? leaveTypeId);
    Task<List<PermissionRequest>> GetMyAsync(Guid employeeId);
    Task<List<LeaveType>> GetActiveLeaveTypesAsync();
    Task<LeaveType?> GetLeaveTypeByIdAsync(Guid id);
    Task<decimal> GetPermissionDaysSumAsync(Guid employeeId, Guid leaveTypeId, string status, int? year);
    Task<decimal> GetMonthlyPermissionDaysAsync(Guid employeeId, Guid leaveTypeId, int year, int month);
    Task AddAsync(PermissionRequest request);
    Task<List<LeaveType>> GetByCompanyAsync(Guid? companyId);
    Task AddLeaveTypeAsync(LeaveType leaveType);
    Task UpdateLeaveTypeAsync(LeaveType leaveType);
    Task SaveChangesAsync();
}
