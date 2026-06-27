namespace Zorvian.Application.DTOs.Tax;

public sealed record RegionalTaxConfigResponse(
    Guid Id,
    string CountryCode,
    string TaxType,
    decimal Rate,
    DateTime EffectiveDate,
    bool IsActive
);

public sealed record CreateRegionalTaxConfigRequest(
    string CountryCode,
    string TaxType,
    decimal Rate,
    DateTime? EffectiveDate = null
);

public sealed record UpdateRegionalTaxConfigRequest(
    string? CountryCode,
    string? TaxType,
    decimal? Rate,
    DateTime? EffectiveDate,
    bool? IsActive
);
