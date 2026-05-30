using Nexora.Application.DTOs.Employee;

namespace Nexora.Application.DTOs.Vacation;

public sealed record CreateVacationRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Comments
);

public sealed record VacationResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    decimal BusinessDays,
    string? Comments,
    string Status,
    string? RejectionReason,
    bool IsAdvanced,
    List<ApprovalStepResponse> ApprovalSteps,
    DateTime CreatedAt
);

public sealed record ApprovalStepResponse(
    int Step,
    string? ApproverName,
    string Status,
    string? Comments,
    DateTime? ApprovedAt
);

public sealed record VacationBalanceResponse(
    decimal TotalDaysPerYear,
    decimal AccruedDays,
    decimal TakenDays,
    decimal PendingDays,
    decimal AvailableDays
);

public sealed record VacationFilterRequest(
    string? Status,
    Guid? EmployeeId,
    int? Year,
    int? Page = 1,
    int? PageSize = 20
);
