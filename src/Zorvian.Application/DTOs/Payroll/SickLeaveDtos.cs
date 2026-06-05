namespace Zorvian.Application.DTOs.Payroll;

public sealed record SickLeaveResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? DiagnosisCode,
    decimal EmployerCoverage,
    decimal InssCoverage,
    string Status
);

public sealed record CreateSickLeaveRequest(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? DiagnosisCode
);
