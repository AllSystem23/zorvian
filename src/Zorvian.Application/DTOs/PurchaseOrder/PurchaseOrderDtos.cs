namespace Zorvian.Application.DTOs.PurchaseOrder;

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    DateTime OrderDate,
    DateOnly? ExpectedDate,
    Guid BranchId,
    string CurrencyCode,
    decimal Discount,
    string? Notes,
    string? CountryCode,
    List<PurchaseOrderLineDto> Details
);

public sealed record UpdatePurchaseOrderRequest(
    Guid? SupplierId,
    DateTime? OrderDate,
    DateOnly? ExpectedDate,
    decimal? Discount,
    string? Notes,
    List<PurchaseOrderLineDto>? Details
);

public sealed record PurchaseOrderLineDto(
    Guid ProductId,
    int QuantityOrdered,
    decimal UnitCost,
    decimal Discount
);

public sealed record ReceivePurchaseOrderRequest(
    Guid PurchaseOrderId,
    List<ReceiveLineDto> Lines
);

public sealed record ReceiveLineDto(
    Guid ProductId,
    int QuantityReceived,
    decimal UnitCost
);

public sealed record PurchaseOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string? SupplierName,
    DateTime OrderDate,
    DateOnly? ExpectedDate,
    string Status,
    decimal Subtotal,
    decimal Tax,
    decimal Discount,
    decimal Total,
    string? Notes,
    Guid BranchId,
    string CurrencyCode,
    string CountryCode,
    Guid? PurchaseId,
    string? PurchaseNumber,
    DateTime CreatedAt,
    List<PurchaseOrderDetailResponse> Details
);

public sealed record PurchaseOrderDetailResponse(
    Guid ProductId,
    string? ProductName,
    string? ProductCode,
    int QuantityOrdered,
    int QuantityReceived,
    decimal UnitCost,
    decimal Discount,
    decimal Subtotal
);
