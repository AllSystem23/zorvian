using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class EmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;

    public EmployeeService(IEmployeeRepository repo, IMapper mapper, ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        var employee = _mapper.Map<Employee>(request);
        employee.EmployeeCode = request.EmployeeCode ?? GenerateEmployeeCode();

        await _repo.AddAsync(employee);

        employee.History.Add(new EmployeeHistory
        {
            EmployeeId = employee.Id,
            ChangeType = "Create",
            FieldName = "Employee",
            NewValue = $"{employee.FirstName} {employee.LastName}",
        });

        await _repo.SaveChangesAsync();

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        var before = CaptureState(employee);

        _mapper.Map(request, employee);

        AddHistoryEntries(employee, before, employee, "Update");

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

        var before = CaptureState(employee);

        _mapper.Map(request, employee);

        AddHistoryEntries(employee, before, employee, "Update");

        await _repo.SaveChangesAsync();

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return false;

        employee.History.Add(new EmployeeHistory
        {
            EmployeeId = id,
            ChangeType = "Delete",
            FieldName = "Status",
            OldValue = employee.Status,
            NewValue = "deleted",
        });

        await _repo.DeleteAsync(employee);
        await _repo.SaveChangesAsync();
        return true;
    }

    private static Dictionary<string, object?> CaptureState(Employee employee)
    {
        return new()
        {
            [nameof(Employee.FirstName)] = employee.FirstName,
            [nameof(Employee.LastName)] = employee.LastName,
            [nameof(Employee.Email)] = employee.Email,
            [nameof(Employee.Phone)] = employee.Phone,
            [nameof(Employee.DateOfBirth)] = employee.DateOfBirth,
            [nameof(Employee.Gender)] = employee.Gender,
            [nameof(Employee.IdentificationType)] = employee.IdentificationType,
            [nameof(Employee.IdentificationNumber)] = employee.IdentificationNumber,
            [nameof(Employee.DepartmentId)] = employee.DepartmentId,
            [nameof(Employee.Position)] = employee.Position,
            [nameof(Employee.HireDate)] = employee.HireDate,
            [nameof(Employee.TerminationDate)] = employee.TerminationDate,
            [nameof(Employee.TerminationReason)] = employee.TerminationReason,
            [nameof(Employee.Salary)] = employee.Salary,
            [nameof(Employee.SalaryType)] = employee.SalaryType,
            [nameof(Employee.Status)] = employee.Status,
            [nameof(Employee.PhotoUrl)] = employee.PhotoUrl,
            [nameof(Employee.BankName)] = employee.BankName,
            [nameof(Employee.BankAccountNumber)] = employee.BankAccountNumber,
            [nameof(Employee.BankAccountType)] = employee.BankAccountType,
        };
    }

    private static void AddHistoryEntries(Employee employee, Dictionary<string, object?> before, Employee after, string changeType)
    {
        foreach (var kvp in before)
        {
            var oldVal = kvp.Value;
            var newVal = typeof(Employee).GetProperty(kvp.Key)?.GetValue(after);

            if (!Equals(oldVal, newVal))
            {
                employee.History.Add(new EmployeeHistory
                {
                    EmployeeId = employee.Id,
                    ChangeType = changeType,
                    FieldName = kvp.Key,
                    OldValue = oldVal?.ToString(),
                    NewValue = newVal?.ToString(),
                });
            }
        }
    }

    private static string GenerateEmployeeCode()
    {
        var random = Random.Shared.Next(1000, 9999);
        return $"EMP-{DateTime.UtcNow:yyyyMMdd}-{random}";
    }
}
