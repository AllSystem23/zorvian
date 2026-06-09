using Zorvian.Core.Enums;

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
    string? CurrencyCode,
    decimal? ExchangeRateToReporting,
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
    QuoteStatus Status,
    string? Notes,
    string CurrencyCode,
    decimal? ExchangeRateToReporting,
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
    QuoteStatus? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record UpdateStatusRequest(
    QuoteStatus Status
);
