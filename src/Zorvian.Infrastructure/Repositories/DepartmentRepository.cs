using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly ZorvianDbContext _db;

    public DepartmentRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Department?> GetByIdAsync(Guid id) =>
        await _db.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<Department>> GetAllAsync() =>
        await _db.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .OrderBy(d => d.Name)
            .ToListAsync();

    public async Task AddAsync(Department department) =>
        await _db.Departments.AddAsync(department);

    public Task UpdateAsync(Department department)
    {
        _db.Departments.Update(department);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Department department)
    {
        _db.Departments.Remove(department);
        return Task.CompletedTask;
    }

    public async Task<bool> HasEmployeesAsync(Guid departmentId) =>
        await _db.Employees.AnyAsync(e => e.DepartmentId == departmentId);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
