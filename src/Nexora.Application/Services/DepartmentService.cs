using Nexora.Application.DTOs.Department;
using Nexora.Application.Interfaces;

namespace Nexora.Application.Services;

public sealed class DepartmentService
{
    private readonly IDepartmentRepository _repo;

    public DepartmentService(IDepartmentRepository repo)
    {
        _repo = repo;
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
    {
        var department = new Core.Entities.Department
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            ManagerId = request.ManagerId,
            ParentDepartmentId = request.ParentDepartmentId,
        };

        await _repo.AddAsync(department);
        await _repo.SaveChangesAsync();
        return await MapToResponse(department);
    }

    public async Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        var department = await _repo.GetByIdAsync(id);
        if (department is null) return null;

        if (request.Name != null) department.Name = request.Name;
        if (request.Code != null) department.Code = request.Code;
        if (request.Description != null) department.Description = request.Description;
        if (request.ManagerId.HasValue) department.ManagerId = request.ManagerId;
        if (request.ParentDepartmentId.HasValue) department.ParentDepartmentId = request.ParentDepartmentId;
        if (request.IsActive.HasValue) department.IsActive = request.IsActive.Value;

        await _repo.SaveChangesAsync();
        return await MapToResponse(department);
    }

    public async Task<List<DepartmentResponse>> GetAllAsync()
    {
        var departments = await _repo.GetAllAsync();
        var responses = new List<DepartmentResponse>();
        foreach (var d in departments)
            responses.Add(await MapToResponse(d));
        return responses;
    }

    public async Task<DepartmentResponse?> GetByIdAsync(Guid id)
    {
        var dept = await _repo.GetByIdAsync(id);
        return dept is null ? null : await MapToResponse(dept);
    }

    public async Task<(bool success, string? error)> DeleteAsync(Guid id)
    {
        var department = await _repo.GetByIdAsync(id);
        if (department is null)
            return (false, "Department not found");

        if (await _repo.HasEmployeesAsync(id))
            return (false, "Cannot delete department with active employees");

        await _repo.DeleteAsync(department);
        await _repo.SaveChangesAsync();
        return (true, null);
    }

    private async Task<DepartmentResponse> MapToResponse(Core.Entities.Department d)
    {
        var dept = await _repo.GetByIdAsync(d.Id);
        return new DepartmentResponse(
            d.Id,
            d.Name,
            d.Code ?? "",
            d.Description ?? "",
            d.Manager != null ? $"{d.Manager.FirstName} {d.Manager.LastName}" : "",
            d.ParentDepartmentId,
            d.IsActive,
            dept?.Employees.Count ?? 0
        );
    }
}
