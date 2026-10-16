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
    /// <summary>
    /// Permisos aprobados que se solapan con el rango (para prorrateo de nómina:
    /// permisos con goce restauran salario, sin goce lo reducen). Incluye LeaveType (IsPaid).
    /// </summary>
    Task<List<PermissionRequest>> GetApprovedOverlappingAsync(DateOnly start, DateOnly end);
    /// <summary>
    /// Suma de días hábiles de permisos aprobados del empleado en el año cuyo LeaveType.Code
    /// esté en la lista — alimenta los buckets SickDaysTaken/PersonalDaysTaken de LeaveBalances.
    /// </summary>
    Task<decimal> GetApprovedDaysByCodesAsync(Guid employeeId, int year, List<string> leaveTypeCodes);
    Task AddAsync(PermissionRequest request);
    Task<List<LeaveType>> GetByCompanyAsync(Guid? companyId);
    Task AddLeaveTypeAsync(LeaveType leaveType);
    Task UpdateLeaveTypeAsync(LeaveType leaveType);
    Task SaveChangesAsync();
}
