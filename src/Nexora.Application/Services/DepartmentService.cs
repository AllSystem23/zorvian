using AutoMapper;
using Nexora.Application.DTOs.Department;
using Nexora.Application.Interfaces;

namespace Nexora.Application.Services;

public sealed class DepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly IMapper _mapper;

    public DepartmentService(IDepartmentRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
    {
        var department = _mapper.Map<Core.Entities.Department>(request);

        await _repo.AddAsync(department);
        await _repo.SaveChangesAsync();

        return _mapper.Map<DepartmentResponse>(department);
    }

    public async Task<DepartmentResponse?> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        var department = await _repo.GetByIdAsync(id);
        if (department is null) return null;

        _mapper.Map(request, department);
        await _repo.SaveChangesAsync();

        return _mapper.Map<DepartmentResponse>(department);
    }

    public async Task<List<DepartmentResponse>> GetAllAsync()
    {
        var departments = await _repo.GetAllAsync();
        return _mapper.Map<List<DepartmentResponse>>(departments);
    }

    public async Task<DepartmentResponse?> GetByIdAsync(Guid id)
    {
        var dept = await _repo.GetByIdAsync(id);
        return dept is null ? null : _mapper.Map<DepartmentResponse>(dept);
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
}
