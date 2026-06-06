namespace Zorvian.Application.DTOs.Warranty;

public sealed record ServiceWorkshopResponse(
    Guid Id,
    Guid BranchId,
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    int AvgResponseHours,
    int AvgRepairHours,
    decimal Rating,
    bool IsActive,
    string? Notes,
    int TechnicianCount
);

public sealed record CreateServiceWorkshopRequest(
    Guid BranchId,
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    int AvgResponseHours = 48,
    int AvgRepairHours = 72,
    string? Notes = null
);

public sealed record UpdateServiceWorkshopRequest(
    string? Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? Country,
    int? AvgResponseHours,
    int? AvgRepairHours,
    bool? IsActive,
    string? Notes
);
