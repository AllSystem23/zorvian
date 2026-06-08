namespace Zorvian.Application.DTOs.Commercial;

public sealed record PurchaseDetailItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitCost,
    decimal Discount,
    decimal Subtotal
);

public sealed record PurchaseResponse(
    Guid Id,
    string PurchaseNumber,
    Guid SupplierId,
    string SupplierName,
    DateTime CreatedAt,
    DateTime? PurchaseDate,
    DateOnly? DueDate,
    string? InvoiceReference,
    string Status,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal Balance,
    string? WithholdingType,
    decimal? WithholdingAmount,
    string? Notes,
    List<PurchaseDetailItem> Details
);

public sealed record PurchaseListResponse(
    Guid Id,
    string PurchaseNumber,
    string SupplierName,
    DateTime CreatedAt,
    string Status,
    decimal Total,
    decimal PaidAmount,
    decimal Balance
);

public sealed record CreatePurchaseRequest(
    Guid SupplierId,
    DateTime? PurchaseDate,
    DateOnly? DueDate,
    string? InvoiceReference,
    string? WithholdingType,
    decimal? WithholdingRate,
    decimal Discount,
    string? Notes,
    Guid BranchId,
    List<PurchaseDetailItem> Details
);

public sealed record UpdatePurchaseRequest(
    Guid? SupplierId,
    DateTime? PurchaseDate,
    DateOnly? DueDate,
    string? InvoiceReference,
    decimal? Discount,
    string? Notes,
    List<PurchaseDetailItem>? Details
);

public sealed record PurchaseFilterRequest(
    Guid? SupplierId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    Guid? BranchId,
    int? Page = 1,
    int? PageSize = 20
);
