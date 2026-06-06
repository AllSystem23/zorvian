namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyCostResponse(
    Guid Id, Guid WarrantyId, Guid? ClaimId,
    string CostCategory, string? Description,
    decimal Quantity, decimal UnitCost,
    string CurrencyCode, decimal ExchangeRate,
    string PaidBy, Guid? PaidByPartyId, string? InvoiceNumber,
    DateOnly? InvoiceDate, bool IsBilled,
    Guid? AccountingEntryId, string? Notes
);

public sealed record CreateWarrantyCostRequest(
    Guid WarrantyId, Guid? ClaimId,
    string CostCategory, string? Description,
    decimal Quantity, decimal UnitCost,
    string PaidBy, Guid? PaidByPartyId = null,
    string? InvoiceNumber = null,
    DateOnly? InvoiceDate = null, bool IsBilled = false,
    string? Notes = null
);

public sealed record UpdateWarrantyCostRequest(
    string? CostCategory, string? Description,
    decimal? Quantity, decimal? UnitCost,
    string? PaidBy, string? InvoiceNumber,
    DateOnly? InvoiceDate, bool? IsBilled,
    string? Notes
);
