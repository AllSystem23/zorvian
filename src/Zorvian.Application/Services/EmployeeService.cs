using AutoMapper;
using MassTransit;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Messages;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class EmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly IProviderRepository _providerRepo;
    private readonly IMapper _mapper;
    private readonly IEncryptionService _encryption;
    private readonly IPublishEndpoint _publishEndpoint;

    public EmployeeService(IEmployeeRepository repo, IProviderRepository providerRepo, IMapper mapper, IEncryptionService encryption, IPublishEndpoint publishEndpoint)
    {
        _repo = repo;
        _providerRepo = providerRepo;
        _mapper = mapper;
        _encryption = encryption;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        var employee = _mapper.Map<Employee>(request);
        employee.EmployeeCode = request.EmployeeCode ?? GenerateEmployeeCode();
        employee.CollaboratorType = request.CollaboratorType ?? "employee";
        EncryptPii(employee);

        await _repo.AddAsync(employee);

        employee.History.Add(new EmployeeHistory
        {
            EmployeeId = employee.Id,
            ChangeType = "Create",
            FieldName = "Employee",
            NewValue = $"{employee.FirstName} {employee.LastName}",
        });

        await _repo.SaveChangesAsync();

        // Link to service contract if CollaboratorType is contractor and ContractId provided
        if (employee.CollaboratorType == "contractor" && request.ContractId.HasValue)
        {
            var contract = await _providerRepo.GetContractByIdAsync(request.ContractId.Value);
            var provider = contract?.ServiceProvider;
            if (provider is not null)
            {
                provider.EmployeeId = employee.Id;
                await _providerRepo.UpdateProviderAsync(provider);
                employee.ServiceProviderDetails = provider;
                await _repo.SaveChangesAsync();
            }
        }

        DecryptPii(employee);

        // Publish MassTransit event after employee creation
        await _publishEndpoint.Publish(new EmployeeCreatedEvent
        {
            EmployeeId = employee.Id,
            CompanyId = employee.CompanyId,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email ?? "",
            DepartmentId = employee.DepartmentId,
            Position = employee.Position ?? "",
            Salary = employee.Salary ?? 0,
            HireDate = employee.HireDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        });

        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        DecryptPii(employee);
        var before = CaptureState(employee);

        _mapper.Map(request, employee);
        EncryptPii(employee);

        AddHistoryEntries(employee, before, employee, "Update");

        await _repo.SaveChangesAsync();

        DecryptPii(employee);
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
        if (employee is null) return null;

        DecryptPii(employee);
        return _mapper.Map<EmployeeResponse>(employee);
    }

    public async Task<EmployeeResponse?> UpdateMyProfileAsync(Guid id, UpdateMyProfileRequest request)
    {
        var employee = await _repo.GetByIdAsync(id);
        if (employee is null) return null;

        DecryptPii(employee);
        var before = CaptureState(employee);

        _mapper.Map(request, employee);
        EncryptPii(employee);

        AddHistoryEntries(employee, before, employee, "Update");

        await _repo.SaveChangesAsync();

        DecryptPii(employee);
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

    private void EncryptPii(Employee employee)
    {
        employee.Phone = _encryption.Encrypt(employee.Phone ?? string.Empty);
        employee.IdentificationNumber = _encryption.Encrypt(employee.IdentificationNumber ?? string.Empty);
        employee.BankName = _encryption.Encrypt(employee.BankName ?? string.Empty);
        employee.BankAccountNumber = _encryption.Encrypt(employee.BankAccountNumber ?? string.Empty);
        employee.BankAccountType = _encryption.Encrypt(employee.BankAccountType ?? string.Empty);
    }

    private void DecryptPii(Employee employee)
    {
        employee.Phone = NullIfEmpty(_encryption.Decrypt(employee.Phone ?? string.Empty));
        employee.IdentificationNumber = NullIfEmpty(_encryption.Decrypt(employee.IdentificationNumber ?? string.Empty));
        employee.BankName = NullIfEmpty(_encryption.Decrypt(employee.BankName ?? string.Empty));
        employee.BankAccountNumber = NullIfEmpty(_encryption.Decrypt(employee.BankAccountNumber ?? string.Empty));
        employee.BankAccountType = NullIfEmpty(_encryption.Decrypt(employee.BankAccountType ?? string.Empty));
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

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
