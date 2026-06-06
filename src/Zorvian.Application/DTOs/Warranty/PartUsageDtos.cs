namespace Zorvian.Application.DTOs.Warranty;

public sealed record CreateWarrantyPartUsageRequest(
    Guid ClaimId,
    Guid PartReceiptId,
    Guid ProductId,
    int QuantityUsed,
    string? Notes = null
);

public sealed record WarrantyPartUsageResponse(
    Guid Id,
    Guid ClaimId,
    Guid PartReceiptId,
    Guid ProductId,
    string ProductName,
    int QuantityUsed,
    decimal UnitCost,
    decimal TotalCost,
    DateTime UsedAt,
    string? Notes
);
