namespace Zorvian.Application.DTOs.CashRegister;

public sealed record OpenCashRegisterRequest(
    string Code,
    decimal OpeningBalance,
    string? Notes,
    Guid BranchId
);

public sealed record CloseCashRegisterRequest(
    decimal ClosingBalance,
    string? Notes
);

public sealed record CashRegisterResponse(
    Guid Id,
    string Code,
    Guid BranchId,
    Guid? EmployeeId,
    string? EmployeeName,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ExpectedBalance,
    decimal Difference,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string Status,
    string? Notes
);

public sealed record CashMovementResponse(
    Guid Id,
    Guid CashRegisterId,
    string MovementType,
    decimal Amount,
    string? Concept,
    string? ReferenceNumber,
    Guid? RelatedSaleId,
    Guid? RelatedCreditPaymentId,
    string? EmployeeName,
    DateTime CreatedAt
);

public sealed record CreateCashMovementRequest(
    Guid CashRegisterId,
    string MovementType,
    decimal Amount,
    string? Concept,
    string? ReferenceNumber
);

public sealed record CashRegisterFilterRequest(
    Guid? BranchId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record ArqueoDenominationRequest(
    string DenominationType,
    decimal DenominationValue,
    int Quantity
);

public sealed record CreateArqueoRequest(
    List<ArqueoDenominationRequest> Denominations,
    string? Notes
);

public sealed record ArqueoDenominationResponse(
    Guid Id,
    string DenominationType,
    decimal DenominationValue,
    int Quantity,
    decimal Total
);

public sealed record CashRegisterArqueoResponse(
    Guid Id,
    Guid CashRegisterId,
    decimal ExpectedBalance,
    decimal CountedTotal,
    decimal Difference,
    string? Notes,
    string EmployeeName,
    DateTime CreatedAt,
    List<ArqueoDenominationResponse> Denominations
);
