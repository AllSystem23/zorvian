namespace Zorvian.Application.DTOs.StockTransfer;

public sealed record CreateStockTransferRequest(
    // Nuevos: bodegas origen/destino (misma sucursal o sucursales distintas).
    // Si vienen, las sucursales se derivan de ellas.
    Guid? FromWarehouseId,
    Guid? ToWarehouseId,
    // Legacy: si no vienen bodegas, se usa la bodega por defecto de la sucursal.
    Guid? FromBranchId,
    Guid? ToBranchId,
    string? Notes,
    List<StockTransferLineDto> Details
);

public sealed record StockTransferLineDto(
    Guid ProductId,
    int Quantity
);

/// <summary>
/// Recepción total (Lines = null u omitido) o parcial por línea.
/// </summary>
public sealed record ReceiveStockTransferRequest(
    List<ReceiveStockTransferLine>? Lines
);

public sealed record ReceiveStockTransferLine(
    Guid DetailId,
    int Quantity
);

public sealed record StockTransferResponse(
    Guid Id,
    string TransferNumber,
    Guid FromWarehouseId,
    string? FromWarehouseName,
    Guid ToWarehouseId,
    string? ToWarehouseName,
    Guid FromBranchId,
    string? FromBranchName,
    Guid ToBranchId,
    string? ToBranchName,
    string Status,
    DateTime? DispatchedAt,
    DateTime? ReceivedAt,
    string? Notes,
    string RequestedBy,
    DateTime CreatedAt,
    List<StockTransferDetailResponse> Details
);

public sealed record StockTransferDetailResponse(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    string? ProductCode,
    int Quantity,
    int QuantityReceived,
    decimal UnitCost
);
