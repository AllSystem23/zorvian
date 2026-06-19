namespace Zorvian.Application.DTOs.Inventory;

public sealed record CreateInventoryMovementRequest(
    Guid ProductId,
    string MovementType,
    int Quantity,
    decimal UnitCost,
    string? ReferenceNumber,
    string? Notes,
    Guid BranchId = default,
    string? SerialNumber = null
);

public sealed record InventoryMovementResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    string MovementType,
    int Quantity,
    int StockBefore,
    int StockAfter,
    decimal UnitCost,
    string? ReferenceNumber,
    string? Notes,
    string? PerformedByName,
    DateTime CreatedAt
);

public sealed record InventoryMovementFilterRequest(
    Guid? ProductId,
    string? MovementType,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    int? Page = 1,
    int? PageSize = 20
);
