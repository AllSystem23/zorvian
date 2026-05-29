namespace Nexora.Application.DTOs.Company;

public sealed record CreateCompanyRequest(
    string Name,
    string LegalName,
    string? TaxId,
    string? Phone,
    string? Address,
    int MaxEmployees = 50,
    string Country = "Nicaragua",
    string Currency = "NIO",
    string Timezone = "America/Managua"
);

public sealed record CompanyResponse(
    Guid Id,
    string Name,
    string LegalName,
    string TaxId,
    string Currency,
    string Timezone,
    int MaxEmployees
);
