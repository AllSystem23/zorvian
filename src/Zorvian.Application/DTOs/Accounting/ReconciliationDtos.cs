namespace Zorvian.Application.DTOs.Accounting;

public sealed record ReconciliationResponse(
    Guid Id,
    Guid BankAccountId,
    string BankAccountName,
    string AccountNumber,
    DateOnly DateFrom,
    DateOnly DateTo,
    DateTime? ReconciledAt,
    string Status,
    int TotalTransactions,
    int MatchedCount,
    int UnmatchedCount,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Difference,
    string? FileName,
    string? Notes,
    DateTime CreatedAt,
    List<ReconciliationDetailResponse> Details
);

public sealed record ReconciliationDetailResponse(
    Guid Id,
    string Reference,
    decimal Amount,
    string TransactionType,
    DateOnly TransactionDate,
    string? Description,
    string SourceType,
    string MatchStatus,
    string? Notes
);

public sealed record ReconciliationListResponse(
    Guid Id,
    string BankAccountName,
    DateOnly DateFrom,
    DateOnly DateTo,
    string Status,
    int TotalTransactions,
    int MatchedCount,
    int UnmatchedCount,
    decimal Difference,
    DateTime CreatedAt
);

public sealed record CreateReconciliationRequest(
    Guid BankAccountId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string? Notes
);

public sealed record UpdateReconciliationRequest(
    string? Status,
    string? Notes
);

public sealed record ReconciliationFilterRequest(
    Guid? BankAccountId,
    string? Status,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? Page = 1,
    int? PageSize = 20
);
