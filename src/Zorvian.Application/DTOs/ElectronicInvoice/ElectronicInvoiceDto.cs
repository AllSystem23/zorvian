namespace Zorvian.Application.DTOs;

public sealed record ElectronicInvoiceDto(
    Guid Id,
    Guid SaleId,
    string CountryCode,
    string InvoiceNumber,
    string AuthorizationCode,
    string Status,
    string? DgiResponse,
    string? ErrorMessage,
    int Attempts,
    DateTime? SubmittedAt,
    DateTime? AuthorizedAt,
    string? PdfUrl,
    DateTime CreatedAt
);

public sealed record IssueInvoiceRequest(
    Guid SaleId,
    string CountryCode
);

public sealed record CancelInvoiceRequest(
    string Reason
);

public sealed record ElectronicInvoiceSummaryDto(
    int Total,
    int Authorized,
    int Pending,
    int Rejected,
    int Cancelled
);
