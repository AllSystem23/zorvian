namespace Zorvian.Application.DTOs.Warranty;

public sealed record CreateWarrantyRequest(
    Guid ClientId,
    Guid ProductId,
    Guid? SaleId,
    int DurationMonths,
    string? Terms,
    Guid BranchId
);

public sealed record WarrantyResponse(
    Guid Id,
    string WarrantyNumber,
    Guid ClientId,
    string ClientName,
    Guid ProductId,
    string ProductName,
    Guid? SaleId,
    string? SaleNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationMonths,
    string? Terms,
    string Status
);

public sealed record WarrantyListResponse(
    Guid Id,
    string WarrantyNumber,
    string ClientName,
    string ProductName,
    DateOnly EndDate,
    string Status
);

public sealed record CreateWarrantyClaimRequest(
    Guid WarrantyId,
    string Description
);

public sealed record WarrantyClaimResponse(
    Guid Id,
    Guid WarrantyId,
    DateOnly ClaimDate,
    string Description,
    string Status,
    string? Resolution,
    DateOnly? ResolutionDate,
    string? ApprovedByName
);

public sealed record WarrantyFilterRequest(
    Guid? ClientId,
    string? Status,
    bool? ExpiringSoon,
    int? Page = 1,
    int? PageSize = 20
);
