namespace Zorvian.Application.DTOs.Commercial;

public sealed record CreateSupplierPaymentRequest(
    Guid PurchaseId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string? Notes
);

public sealed record SupplierPaymentResponse(
    Guid Id,
    Guid PurchaseId,
    string PurchaseNumber,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    DateTime PaymentDate,
    string? Notes
);

public sealed record CreateSupplierCreditNoteRequest(
    Guid SupplierId,
    Guid? PurchaseId,
    DateOnly CreditNoteDate,
    string? Reason,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    Guid BranchId,
    List<SaleDetailItem>? Details
);

public sealed record SupplierCreditNoteResponse(
    Guid Id,
    string CreditNoteNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseId,
    DateOnly CreditNoteDate,
    string? Reason,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string Status
);

public sealed record WithholdingResponse(
    Guid Id,
    Guid PurchaseId,
    string PurchaseNumber,
    string WithholdingType,
    decimal Rate,
    decimal BaseAmount,
    decimal Amount,
    string? CertificateNumber,
    string Status
);

public sealed record ApAgingResponse(
    Guid SupplierId,
    string SupplierName,
    decimal Current,
    decimal Days30,
    decimal Days60,
    decimal Days90,
    decimal Days90Plus,
    decimal TotalDue
);
