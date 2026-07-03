namespace Zorvian.Application.DTOs.Accounting;

public sealed record BudgetDetailResponse(
    Guid Id,
    Guid BudgetId,
    Guid AccountId,
    string AccountName,
    string AccountCode,
    Guid? CostCenterId,
    string? CostCenterName,
    decimal BudgetedAmount,
    string? Description,
    int Month,
    int Year
);

public sealed record CreateBudgetDetailRequest(
    Guid BudgetId,
    Guid AccountId,
    Guid? CostCenterId,
    decimal BudgetedAmount,
    string? Description,
    int Month,
    int Year
);

public sealed record UpdateBudgetDetailRequest(
    decimal? BudgetedAmount,
    string? Description
);

public sealed record BudgetTrackingResponse(
    Guid Id,
    Guid BudgetDetailId,
    Guid AccountId,
    string AccountName,
    string AccountCode,
    decimal BudgetedAmount,
    decimal ActualAmount,
    decimal Variance,
    decimal VariancePercentage,
    int Month,
    int Year,
    string? SourceReference,
    string? Notes
);

public sealed record CreateBudgetTrackingRequest(
    Guid BudgetDetailId,
    decimal ActualAmount,
    string? SourceReference,
    string? Notes
);

public sealed record BudgetTrackingFilterRequest(
    Guid? BudgetDetailId,
    int? Year,
    int? Month,
    int? Page = 1,
    int? PageSize = 20
);
