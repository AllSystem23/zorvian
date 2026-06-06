namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyProviderResponse(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string Type,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    string? Website,
    int AvgResponseHours,
    bool IsActive,
    string? Notes,
    int ContactCount
);

public sealed record CreateWarrantyProviderRequest(
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string Type,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    string? Website,
    int AvgResponseHours = 96,
    string? Notes = null
);

public sealed record UpdateWarrantyProviderRequest(
    string? Name,
    string? LegalName,
    string? TaxId,
    string? Type,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    string? Website,
    int? AvgResponseHours,
    bool? IsActive,
    string? Notes
);
