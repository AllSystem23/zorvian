using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly NexoraDbContext _db;

    public EmployeeRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<Employee?> GetByIdAsync(Guid id) =>
        await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<Employee>> GetFilteredAsync(
        string? search, string? status, Guid? departmentId, int page, int pageSize)
    {
        var query = _db.Employees
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(s) ||
                e.LastName.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        return await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        string? search, string? status, Guid? departmentId)
    {
        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(s) ||
                e.LastName.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                (e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        return await query.CountAsync();
    }

    public async Task<List<EmployeeSupervisor>> GetSupervisorsAsync(Guid employeeId) =>
        await _db.EmployeeSupervisors
            .Include(es => es.Supervisor)
            .Where(es => es.EmployeeId == employeeId)
            .ToListAsync();

    public async Task<Employee?> GetByEmployeeCodeAsync(string code) =>
        await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeCode == code && e.Status == "active");

    public async Task<List<Employee>> SearchByCodeAsync(string partialCode, int maxResults)
    {
        var s = partialCode.ToLower();
        return await _db.Employees
            .Include(e => e.Department)
            .Where(e => e.EmployeeCode != null && e.EmployeeCode.ToLower().Contains(s) && e.Status == "active")
            .OrderBy(e => e.EmployeeCode)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task AddAsync(Employee employee) =>
        await _db.Employees.AddAsync(employee);

    public Task UpdateAsync(Employee employee)
    {
        _db.Employees.Update(employee);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Employee employee)
    {
        _db.Employees.Remove(employee);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
