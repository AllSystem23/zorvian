namespace Zorvian.Application.DTOs.Employee;

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? EmployeeCode,
    DateOnly? DateOfBirth,
    string? Gender,
    string? IdentificationType,
    string? IdentificationNumber,
    Guid? DepartmentId,
    string? Position,
    DateOnly? HireDate,
    decimal? Salary,
    string? SalaryType,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountType
);

public sealed record UpdateEmployeeRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? EmployeeCode,
    DateOnly? DateOfBirth,
    string? Gender,
    string? IdentificationType,
    string? IdentificationNumber,
    Guid? DepartmentId,
    string? Position,
    decimal? Salary,
    string? SalaryType,
    string? Status,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountType
);

public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly? DateOfBirth,
    string Gender,
    string IdentificationType,
    string IdentificationNumber,
    Guid? DepartmentId,
    string DepartmentName,
    string Position,
    DateOnly HireDate,
    string Status,
    decimal? Salary,
    string SalaryType,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountType
);

public sealed record EmployeeListResponse(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string DepartmentName,
    string Position,
    string Status,
    DateOnly HireDate
);

public sealed record EmployeeFilterRequest(
    string? Search,
    string? Status,
    Guid? DepartmentId,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record UpdateMyProfileRequest(
    string? Phone,
    string? PhotoUrl
);

// PagedResult moved to Nexora.Application.DTOs.Common.PagedResult<T>
