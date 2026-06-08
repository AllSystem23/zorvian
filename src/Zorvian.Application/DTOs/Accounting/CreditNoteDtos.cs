namespace Zorvian.Application.DTOs.Accounting;

public sealed record CreditNoteResponse(
    Guid Id,
    string CreditNoteNumber,
    Guid SaleId,
    string InvoiceNumber,
    DateTime IssueDate,
    string Status,
    string Reason,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    List<CreditNoteDetailResponse> Details
);

public sealed record CreditNoteDetailResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal Tax,
    decimal Total
);

public sealed record CreateCreditNoteRequest(
    Guid SaleId,
    string Reason,
    List<CreateCreditNoteDetailItem> Details
);

public sealed record CreateCreditNoteDetailItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
