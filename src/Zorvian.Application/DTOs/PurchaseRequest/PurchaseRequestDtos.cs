namespace Zorvian.Application.DTOs.PurchaseRequest;

public sealed record CreatePurchaseRequestRequest(
    string Title,
    string? Priority,
    DateOnly? NeededByDate,
    Guid? BranchId,
    string? Notes,
    List<PurchaseRequestLineDto> Details
);

public sealed record PurchaseRequestLineDto(
    Guid ProductId,
    int Quantity,
    decimal EstimatedUnitPrice,
    string? Notes
);

public sealed record RejectPurchaseRequestRequest(string? Reason);

public sealed record PurchaseRequestResponse(
    Guid Id,
    string RequestNumber,
    string Title,
    string Priority,
    DateOnly? NeededByDate,
    string Status,
    string? RejectionReason,
    decimal EstimatedTotal,
    string? Notes,
    Guid BranchId,
    string? BranchName,
    string RequestedBy,
    DateTime CreatedAt,
    List<PurchaseRequestDetailResponse> Details
);

public sealed record PurchaseRequestDetailResponse(
    Guid ProductId,
    string? ProductName,
    string? ProductCode,
    int Quantity,
    decimal EstimatedUnitPrice,
    decimal Subtotal,
    string? Notes
);
