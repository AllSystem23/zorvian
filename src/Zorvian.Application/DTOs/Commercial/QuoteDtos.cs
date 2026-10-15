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

// QuoteType viaja como string en minúsculas: "cotizacion" (default) o "proforma".
public sealed record CreateQuoteRequest(
    Guid ClientId,
    Guid? EmployeeId,
    DateOnly? ExpirationDate,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    string? CurrencyCode,
    decimal? ExchangeRateToReporting,
    string? QuoteType,
    List<QuoteDetailItem> Details
);

// Status viaja en minúsculas: pending/sent/accepted/rejected/expired/converted.
// QuoteType viaja en minúsculas: cotizacion/proforma.
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
    string QuoteType,
    string? Notes,
    string CurrencyCode,
    decimal? ExchangeRateToReporting,
    List<QuoteDetailItem> Details
);

// QuoteType null conserva el valor actual del documento.
public sealed record UpdateQuoteRequest(
    Guid ClientId,
    Guid? EmployeeId,
    DateOnly? ExpirationDate,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    string? QuoteType,
    List<QuoteDetailItem> Details
);

public sealed record QuoteFilterRequest(
    Guid? ClientId,
    QuoteStatus? Status,
    QuoteType? QuoteType,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    Guid? BranchId,
    int? Page = 1,
    int? PageSize = 20
);

// Estado en minúsculas: pending/sent/accepted/rejected/expired/converted.
public sealed record UpdateStatusRequest(
    string Status
);
