using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class EmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly IMapper _mapper;

    public EmployeeService(IEmployeeRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        var employee = _mapper.Map<Employee>(request);
        employee.EmployeeCode = request.EmployeeCode ?? GenerateEmployeeCode();

        await _repo.AddAsync(employee);
        await _repo.SaveChangesAsync();

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        _mapper.Map(request, employee);
        await _repo.SaveChangesAsync();

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<PagedResult<EmployeeListResponse>> GetFilteredAsync(
        EmployeeFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var items = await _repo.GetFilteredAsync(
            filter.Search, filter.Status, filter.DepartmentId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(
            filter.Search, filter.Status, filter.DepartmentId);

        return new PagedResult<EmployeeListResponse>(
            _mapper.Map<List<EmployeeListResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<EmployeeResponse?> GetByIdAsync(Guid id)
    {
        var employee = await _repo.GetByIdAsync(id);
        return employee is null ? null : _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<EmployeeResponse?> UpdateMyProfileAsync(Guid id, UpdateMyProfileRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        _mapper.Map(request, employee);
        await _repo.SaveChangesAsync();

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return false;

        await _repo.DeleteAsync(employee);
        await _repo.SaveChangesAsync();
        return true;
    }

    private static string GenerateEmployeeCode()
    {
        var random = Random.Shared.Next(1000, 9999);
        return $"EMP-{DateTime.UtcNow:yyyyMMdd}-{random}";
    }
}
