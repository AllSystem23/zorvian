namespace Zorvian.Application.DTOs.Accounting;

public sealed record FiscalYearResponse(
    Guid Id,
    int Year,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTime? OpenedAt,
    DateTime? ClosedAt,
    DateTime? AuditedAt,
    string? AuditedBy
);

public sealed record OpenFiscalYearRequest(
    int Year,
    DateOnly? StartDate,
    DateOnly? EndDate
);
