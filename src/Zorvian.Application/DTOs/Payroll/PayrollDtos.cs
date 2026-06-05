using Zorvian.Application.DTOs.Employee;

namespace Zorvian.Application.DTOs.Payroll;

public sealed record DeductionTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string CalculationMethod,
    decimal? Rate,
    decimal? FixedAmount,
    bool IsMandatory,
    bool IsActive,
    int? Priority
);

public sealed record CreateDeductionTypeRequest(
    string Code,
    string Name,
    string CalculationMethod,
    decimal? Rate,
    decimal? FixedAmount,
    bool IsMandatory,
    int? Priority
);

public sealed record UpdateDeductionTypeRequest(
    string? Name,
    decimal? Rate,
    decimal? FixedAmount,
    bool? IsActive,
    int? Priority
);

public sealed record EmployeeSalaryResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    decimal BaseSalary,
    string SalaryType,
    DateOnly EffectiveDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes
);

public sealed record CreateEmployeeSalaryRequest(
    Guid EmployeeId,
    decimal BaseSalary,
    string SalaryType,
    DateOnly EffectiveDate,
    string? Notes
);

public sealed record PayrollPeriodResponse(
    Guid Id,
    string Name,
    int Year,
    int Month,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly PaymentDate,
    string Status
);

public sealed record CreatePayrollPeriodRequest(
    string Name,
    int Year,
    int Month,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly PaymentDate
);

public sealed record PayrollRunResponse(
    Guid Id,
    Guid PayrollPeriodId,
    string PeriodName,
    string Status,
    decimal TotalSalaries,
    decimal TotalDeductions,
    decimal TotalNetPay,
    int EmployeeCount,
    DateTime? ProcessedAt,
    string? Notes,
    List<PayrollDetailResponse> Details
);

public sealed record PayrollDetailResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    decimal BaseSalary,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    string? InssCode,
    decimal? InssDeduction,
    decimal? IrDeduction,
    decimal? OtherDeductions,
    string PaymentStatus,
    string Currency,
    List<PayrollDetailConceptResponse>? Concepts = null
);

public sealed record PayrollDetailConceptResponse(
    string Code,
    string Description,
    decimal Amount,
    bool IsEmployerCost
);

public sealed record EmployeeBankAccountResponse(
    Guid Id,
    Guid EmployeeId,
    string BankName,
    string AccountNumber,
    string AccountType,
    string AccountCurrency,
    bool IsDefault,
    bool IsActive
);

public sealed record CreateEmployeeBankAccountRequest(
    Guid EmployeeId,
    string BankName,
    string AccountNumber,
    string AccountType,
    string AccountCurrency,
    bool IsDefault
);

public sealed record UpdateEmployeeBankAccountRequest(
    string? BankName,
    string? AccountNumber,
    string? AccountType,
    string? AccountCurrency,
    bool? IsDefault,
    bool? IsActive
);

public sealed record UpdatePayrollDetailRequest(
    decimal? BaseSalary,
    decimal? GrossPay,
    decimal? TotalDeductions,
    decimal? NetPay,
    decimal? InssDeduction,
    decimal? IrDeduction,
    decimal? OtherDeductions,
    string? Details
);

public sealed record GeneratePayrollRequest(
    Guid PayrollPeriodId,
    string? Notes
);

public sealed record AchExportResult(
    byte[] Content,
    string FileName
);
