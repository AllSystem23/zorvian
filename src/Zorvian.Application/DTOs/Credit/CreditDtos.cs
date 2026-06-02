namespace Zorvian.Application.DTOs.Credit;

public sealed record CreditResponse(
    Guid Id,
    string CreditNumber,
    Guid ClientId,
    string ClientName,
    Guid? SaleId,
    Guid? EmployeeId,
    string? EmployeeName,
    decimal FinancedAmount,
    decimal InterestRate,
    int InstallmentCount,
    decimal InstallmentAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal Balance,
    decimal InterestAmount,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? NextDueDate,
    string Status,
    string? Notes,
    List<CreditInstallmentResponse> Installments
);

public sealed record CreditListResponse(
    Guid Id,
    string CreditNumber,
    string ClientName,
    decimal FinancedAmount,
    decimal Balance,
    decimal PaidAmount,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status
);

public sealed record CreditInstallmentResponse(
    Guid Id,
    int InstallmentNumber,
    DateOnly DueDate,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal PaidAmount,
    decimal Balance,
    string Status
);

public sealed record CreateCreditPaymentRequest(
    Guid CreditId,
    Guid? CreditInstallmentId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    Guid? CashRegisterId
);

public sealed record CreditPaymentResponse(
    Guid Id,
    Guid CreditId,
    Guid? CreditInstallmentId,
    decimal Amount,
    decimal PrincipalAmount,
    decimal InterestAmount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateTime PaymentDate,
    string? EmployeeName
);

public sealed record CreditFilterRequest(
    Guid? ClientId,
    string? Status,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record CreditPaymentFilterRequest(
    Guid CreditId,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record LateFeeResponse(
    Guid Id,
    Guid CreditInstallmentId,
    Guid CreditId,
    int DaysOverdue,
    decimal FeeAmount,
    decimal InterestAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal Balance,
    string Status,
    DateOnly CalculatedAt,
    DateTime? PaidAt,
    string? Notes
);

public sealed record CollectionActionResponse(
    Guid Id,
    Guid CreditId,
    Guid EmployeeId,
    string EmployeeName,
    string ActionType,
    string? Description,
    DateTime ActionDate,
    DateOnly? FollowUpDate,
    string? ContactPerson,
    string? ContactPhone,
    string? PromiseAmount,
    DateOnly? PromiseDate,
    string Status,
    string? Result
);

public sealed record CreateCollectionActionRequest(
    Guid CreditId,
    string ActionType,
    string? Description,
    DateOnly? FollowUpDate,
    string? ContactPerson,
    string? ContactPhone,
    string? PromiseAmount,
    DateOnly? PromiseDate,
    string? Result
);

public sealed record CalculateLateFeeRequest(
    Guid CreditId,
    decimal? DailyInterestRate
);

public sealed record OverdueInstallmentResponse(
    Guid Id,
    int InstallmentNumber,
    DateOnly DueDate,
    decimal Amount,
    decimal Balance,
    int DaysOverdue,
    string Status
);
