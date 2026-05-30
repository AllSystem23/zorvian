using Nexora.Application.DTOs.Employee;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;

namespace Nexora.Application.Services;

public sealed class EmployeeService
{
    private readonly IEmployeeRepository _repo;

    public EmployeeService(IEmployeeRepository repo)
    {
        _repo = repo;
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        var employee = new Employee
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            EmployeeCode = request.EmployeeCode ?? GenerateEmployeeCode(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            IdentificationType = request.IdentificationType,
            IdentificationNumber = request.IdentificationNumber,
            DepartmentId = request.DepartmentId,
            Position = request.Position,
            HireDate = request.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Salary = request.Salary,
            SalaryType = request.SalaryType ?? "monthly",
        };

        await _repo.AddAsync(employee);
        await _repo.SaveChangesAsync();

        return MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        if (request.FirstName != null) employee.FirstName = request.FirstName;
        if (request.LastName != null) employee.LastName = request.LastName;
        if (request.Email != null) employee.Email = request.Email;
        if (request.Phone != null) employee.Phone = request.Phone;
        if (request.EmployeeCode != null) employee.EmployeeCode = request.EmployeeCode;
        if (request.DateOfBirth.HasValue) employee.DateOfBirth = request.DateOfBirth;
        if (request.Gender != null) employee.Gender = request.Gender;
        if (request.IdentificationType != null) employee.IdentificationType = request.IdentificationType;
        if (request.IdentificationNumber != null) employee.IdentificationNumber = request.IdentificationNumber;
        if (request.DepartmentId.HasValue) employee.DepartmentId = request.DepartmentId;
        if (request.Position != null) employee.Position = request.Position;
        if (request.Salary.HasValue) employee.Salary = request.Salary;
        if (request.SalaryType != null) employee.SalaryType = request.SalaryType;
        if (request.Status != null) employee.Status = request.Status;

        await _repo.SaveChangesAsync();
        return MapToResponse(employee);
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
            items.Select(MapToListResponse).ToList(),
            total, page, pageSize
        );
    }

    public async Task<EmployeeResponse?> GetByIdAsync(Guid id)
    {
        var employee = await _repo.GetByIdAsync(id);
        return employee is null ? null : MapToResponse(employee);
    }

    public async Task<EmployeeResponse?> UpdateMyProfileAsync(Guid id, UpdateMyProfileRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        if (request.Phone != null) employee.Phone = request.Phone;
        if (request.PhotoUrl != null) employee.PhotoUrl = request.PhotoUrl;

        await _repo.SaveChangesAsync();
        return MapToResponse(employee);
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

    private static EmployeeResponse MapToResponse(Employee e) => new(
        e.Id,
        e.EmployeeCode ?? "",
        e.FirstName,
        e.LastName,
        e.Email,
        e.Phone ?? "",
        e.DateOfBirth,
        e.Gender ?? "",
        e.IdentificationType ?? "",
        e.IdentificationNumber ?? "",
        e.DepartmentId,
        e.Department?.Name ?? "",
        e.Position ?? "",
        e.HireDate,
        e.Status,
        e.Salary,
        e.SalaryType ?? ""
    );

    private static EmployeeListResponse MapToListResponse(Employee e) => new(
        e.Id,
        e.EmployeeCode ?? "",
        $"{e.FirstName} {e.LastName}",
        e.Email,
        e.Department?.Name ?? "",
        e.Position ?? "",
        e.Status,
        e.HireDate
    );
}
