using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly NexoraDbContext _db;

    public PermissionRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<PermissionRequest?> GetByIdAsync(Guid id)
    {
        return await _db.PermissionRequests
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .Include(p => p.Approver)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<PermissionRequest>> GetFilteredAsync(
        string? status, Guid? employeeId, Guid? leaveTypeId, int page, int pageSize)
    {
        var query = _db.PermissionRequests
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        if (leaveTypeId.HasValue)
            query = query.Where(p => p.LeaveTypeId == leaveTypeId.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? status, Guid? employeeId, Guid? leaveTypeId)
    {
        var query = _db.PermissionRequests.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (employeeId.HasValue)
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        if (leaveTypeId.HasValue)
            query = query.Where(p => p.LeaveTypeId == leaveTypeId.Value);

        return await query.CountAsync();
    }

    public async Task<List<PermissionRequest>> GetMyAsync(Guid employeeId)
    {
        return await _db.PermissionRequests
            .Include(p => p.LeaveType)
            .Where(p => p.EmployeeId == employeeId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<LeaveType>> GetActiveLeaveTypesAsync()
    {
        return await _db.LeaveTypes
            .Where(lt => !lt.IsDeleted)
            .OrderBy(lt => lt.Name)
            .ToListAsync();
    }

    public async Task<LeaveType?> GetLeaveTypeByIdAsync(Guid id)
    {
        return await _db.LeaveTypes.FindAsync(id);
    }

    public async Task<decimal> GetPermissionDaysSumAsync(
        Guid employeeId, Guid leaveTypeId, string status, int? year)
    {
        var query = _db.PermissionRequests
            .Where(p => p.EmployeeId == employeeId
                && p.LeaveTypeId == leaveTypeId
                && p.Status == status);

        if (year.HasValue)
            query = query.Where(p => p.StartDate.Year == year.Value);

        return await query.SumAsync(p => p.TotalDays);
    }

    public async Task<decimal> GetMonthlyPermissionDaysAsync(
        Guid employeeId, Guid leaveTypeId, int year, int month)
    {
        return await _db.PermissionRequests
            .Where(p => p.EmployeeId == employeeId
                && p.LeaveTypeId == leaveTypeId
                && p.StartDate.Year == year
                && p.StartDate.Month == month
                && (p.Status == "approved" || p.Status == "pending"))
            .SumAsync(p => p.TotalDays);
    }

    public async Task AddAsync(PermissionRequest request)
    {
        await _db.PermissionRequests.AddAsync(request);
    }

    public async Task<List<LeaveType>> GetByCompanyAsync(Guid? companyId)
    {
        return await _db.LeaveTypes
            .Where(lt => lt.CompanyId == null || lt.CompanyId == companyId)
            .OrderBy(lt => lt.Name)
            .ToListAsync();
    }

    public async Task AddLeaveTypeAsync(LeaveType leaveType)
    {
        await _db.LeaveTypes.AddAsync(leaveType);
    }

    public async Task UpdateLeaveTypeAsync(LeaveType leaveType)
    {
        _db.LeaveTypes.Update(leaveType);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
