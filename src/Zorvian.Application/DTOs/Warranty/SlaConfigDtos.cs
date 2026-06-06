namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantySlaConfigResponse(
    Guid Id,
    string Name,
    string? CoverageType,
    string? Priority,
    int TotalHours,
    int? WorkshopHours,
    int? ProviderHours,
    int? DeliveryHours,
    int AlertThresholdPct,
    bool IsActive
);

public sealed record CreateWarrantySlaConfigRequest(
    string Name,
    string? CoverageType,
    string? Priority,
    int TotalHours,
    int? WorkshopHours,
    int? ProviderHours,
    int? DeliveryHours,
    int AlertThresholdPct
);

public sealed record UpdateWarrantySlaConfigRequest(
    string? Name,
    string? CoverageType,
    string? Priority,
    int? TotalHours,
    int? WorkshopHours,
    int? ProviderHours,
    int? DeliveryHours,
    int? AlertThresholdPct,
    bool? IsActive
);
