namespace Zorvian.Application.DTOs.Warranty;

public sealed record CreateWarrantyRequest(
    Guid ClientId,
    Guid ProductId,
    Guid? SaleId,
    int DurationMonths,
    string? Terms,
    Guid BranchId,
    Guid? BrandId,
    Guid? CategoryId,
    string? SerialNumber,
    string? Imei,
    string? LotNumber
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
    Guid? BrandId,
    string? BrandName,
    Guid? CategoryId,
    string? CategoryName,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationMonths,
    string? Terms,
    string? SerialNumber,
    string? Imei,
    string? LotNumber,
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
    string Description,
    string? Accessories = null,
    string? FailureType = null,
    string? FailureDescription = null,
    string Priority = "medium",
    string? ProductCondition = null
);

public sealed record UpdateWarrantyClaimRequest(
    string? Description,
    string? Accessories,
    string? FailureType,
    string? FailureDescription,
    string? Priority,
    string? ProductCondition
);

public sealed record WarrantyClaimResponse(
    Guid Id,
    Guid WarrantyId,
    DateOnly ClaimDate,
    string Description,
    string Status,
    string? Resolution,
    DateOnly? ResolutionDate,
    string? ApprovedByName,
    string? Accessories,
    string? FailureType,
    string? FailureDescription,
    string Priority,
    string? ProductCondition,
    Guid? WorkshopId,
    string? WorkshopName,
    Guid? TechnicianId,
    string? TechnicianName,
    Guid? ProviderId,
    string? ProviderName,
    DateTime? WorkshopAssignedAt,
    DateTime? SlaDeadline,
    DateTime? SlaBreachedAt,
    DateTime? ProviderReferredAt,
    string? ProviderAuthorizationCode
);

public sealed record WarrantyFilterRequest(
    Guid? ClientId,
    string? Status,
    bool? ExpiringSoon,
    Guid? BranchId,
    int? Page = 1,
    int? PageSize = 20
);
