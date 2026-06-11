namespace Zorvian.Application.DTOs.Partner;

public sealed record PartnerDto(
    Guid Id,
    string Code,
    string Name,
    string LegalName,
    string TaxId,
    string PartnerType,
    string? Email,
    string? Phone,
    string? Address,
    string CountryCode,
    string? City,
    string Status,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? CommissionRate,
    int ClientsReferred,
    decimal RevenueGenerated,
    DateTime? CertifiedAt,
    DateTime? LastActivityAt,
    DateTime CreatedAt
);

public sealed record CreatePartnerRequest(
    string Code,
    string Name,
    string LegalName,
    string TaxId,
    string PartnerType,
    string? Email,
    string? Phone,
    string? Address,
    string CountryCode,
    string? City,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? CommissionRate,
    string? Notes
);

public sealed record UpdatePartnerRequest(
    string? Name,
    string? LegalName,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? CommissionRate,
    string? Notes
);

public sealed record PartnerListDto(
    Guid Id,
    string Code,
    string Name,
    string PartnerType,
    string CountryCode,
    string Status,
    int ClientsReferred,
    DateTime? LastActivityAt
);
