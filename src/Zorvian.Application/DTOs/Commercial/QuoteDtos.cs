namespace Zorvian.Application.DTOs.Commercial;

public sealed record QuoteDetailItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Subtotal
);

public sealed record CreateQuoteRequest(
    Guid ClientId,
    Guid? EmployeeId,
    DateOnly? ExpirationDate,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<QuoteDetailItem> Details
);

public sealed record QuoteResponse(
    Guid Id,
    string QuoteNumber,
    Guid ClientId,
    string ClientName,
    Guid? EmployeeId,
    string? EmployeeName,
    DateOnly QuoteDate,
    DateOnly? ExpirationDate,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    string Status,
    string? Notes,
    List<QuoteDetailItem> Details
);

public sealed record UpdateQuoteRequest(
    Guid ClientId,
    Guid? EmployeeId,
    DateOnly? ExpirationDate,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<QuoteDetailItem> Details
);

public sealed record QuoteFilterRequest(
    Guid? ClientId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page = 1,
    int? PageSize = 20
);
