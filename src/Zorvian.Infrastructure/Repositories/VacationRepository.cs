using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class VacationRepository : IVacationRepository
{
    private readonly ZorvianDbContext _db;

    public VacationRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<VacationRequest?> GetByIdAsync(Guid id)
    {
        return await _db.VacationRequests
            .Include(v => v.Employee)
            .Include(v => v.ApprovalSteps).ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<VacationRequest>> GetFilteredAsync(
        string? status, Guid? employeeId, int? year, int page, int pageSize)
    {
        var query = _db.VacationRequests
            .Include(v => v.Employee)
            .Include(v => v.ApprovalSteps).ThenInclude(a => a.Approver)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);
        if (employeeId.HasValue)
            query = query.Where(v => v.EmployeeId == employeeId.Value);
        if (year.HasValue)
            query = query.Where(v => v.StartDate.Year == year.Value || v.EndDate.Year == year.Value);

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? status, Guid? employeeId, int? year)
    {
        var query = _db.VacationRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);
        if (employeeId.HasValue)
            query = query.Where(v => v.EmployeeId == employeeId.Value);
        if (year.HasValue)
            query = query.Where(v => v.StartDate.Year == year.Value || v.EndDate.Year == year.Value);

        return await query.CountAsync();
    }

    public async Task<decimal> GetVacationDaysSumAsync(Guid employeeId, string status)
    {
        return await _db.VacationRequests
            .Where(v => v.EmployeeId == employeeId && v.Status == status)
            .SumAsync(v => v.TotalDays);
    }

    public async Task<int> GetOverlappingCountAsync(Guid departmentId, DateOnly start, DateOnly end, Guid excludeEmployeeId)
    {
        return await _db.VacationRequests
            .Where(v => v.Status == "approved"
                && v.Employee.DepartmentId == departmentId
                && v.EmployeeId != excludeEmployeeId
                && v.StartDate <= end
                && v.EndDate >= start)
            .CountAsync();
    }

    public async Task AddAsync(VacationRequest request)
    {
        await _db.VacationRequests.AddAsync(request);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
