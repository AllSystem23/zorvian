namespace Zorvian.Application.DTOs.Accounting;

public sealed record BudgetResponse(
    Guid Id,
    int Year,
    int Month,
    Guid AccountId,
    string AccountName,
    string AccountCode,
    Guid? CostCenterId,
    string? CostCenterName,
    string? CostCenterCode,
    decimal BudgetedAmount
);

public sealed record CreateBudgetRequest(
    int Year,
    int Month,
    Guid AccountId,
    Guid? CostCenterId,
    decimal BudgetedAmount
);

public sealed record UpdateBudgetRequest(
    int? Year,
    int? Month,
    Guid? AccountId,
    Guid? CostCenterId,
    decimal? BudgetedAmount
);
