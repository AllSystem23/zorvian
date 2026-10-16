namespace Zorvian.Application.DTOs.StockTransfer;

public sealed record CreateStockTransferRequest(
    Guid? FromBranchId,
    Guid? ToBranchId,
    string? Notes,
    List<StockTransferLineDto> Details
);

public sealed record StockTransferLineDto(
    Guid ProductId,
    int Quantity
);

public sealed record StockTransferResponse(
    Guid Id,
    string TransferNumber,
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
    Guid ProductId,
    string? ProductName,
    string? ProductCode,
    int Quantity,
    decimal UnitCost
);
