namespace Zorvian.Application.DTOs.Treasury;

public sealed record GenerateCheckEntryRequest(
    Guid CheckId,
    decimal Amount,
    string CheckType,
    Guid? BankAccountId,
    Guid? PayeeId,
    Guid? CostCenterId
);

public sealed record GenerateBankDepositRequest(
    Guid BankMovementId,
    decimal Amount,
    Guid BankAccountId,
    Guid? CostCenterId
);

public sealed record GenerateBankTransferRequest(
    Guid BankMovementId,
    decimal Amount,
    Guid FromAccountId,
    Guid ToAccountId,
    Guid? CostCenterId
);

public sealed record GenerateBankCommissionRequest(
    Guid BankMovementId,
    decimal Commission,
    Guid BankAccountId,
    Guid? CostCenterId
);

public sealed record GenerateCollectionRequest(
    Guid PaymentId,
    decimal Amount,
    decimal Interest,
    decimal LateFee,
    Guid InvoiceId,
    Guid? CostCenterId
);

public sealed record GenerateAdvanceToSupplierRequest(
    Guid AdvanceId,
    decimal Amount,
    Guid SupplierId,
    Guid? CostCenterId
);

public sealed record GenerateSupplierAdvanceApplicationRequest(
    Guid ApplicationId,
    decimal Amount,
    Guid AdvanceId,
    Guid PurchaseId,
    Guid? CostCenterId
);

public sealed record TreasuryEntryResponse(
    Guid EntryId,
    string Message
);
