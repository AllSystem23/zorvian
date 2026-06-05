namespace Zorvian.Application.DTOs.Company;

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
    string? Address,
    string? Phone,
    string? Email,
    string? LogoUrl,
    string Currency,
    string Timezone,
    int MaxEmployees
);

public sealed record UpdateCompanyRequest(
    string? Name,
    string? LegalName,
    string? TaxId,
    string? Phone,
    string? Address,
    string? Currency,
    string? Timezone,
    string? LogoUrl
);

public sealed record CompanySettingsResponse(
    int VacationDaysPerYear,
    string VacationAccrualMethod,
    int LateToleranceMinutes,
    decimal WorkingHoursPerDay,
    string WorkingDays,
    bool OvertimeEnabled,
    string? Timezone,
    string Currency,
    string? DateFormat,
    string? ApprovalFlowConfig,
    decimal LateFeeDailyRate,
    decimal LateFeePercentage,
    int LateFeeGracePeriod,
    bool TaxEnabled,
    decimal TaxRate
);

public sealed record UpdateCompanySettingsRequest(
    int? VacationDaysPerYear,
    string? VacationAccrualMethod,
    int? LateToleranceMinutes,
    decimal? WorkingHoursPerDay,
    string? WorkingDays,
    bool? OvertimeEnabled,
    string? Timezone,
    string? Currency,
    string? DateFormat,
    string? ApprovalFlowConfig,
    decimal? LateFeeDailyRate,
    decimal? LateFeePercentage,
    int? LateFeeGracePeriod,
    bool? TaxEnabled,
    decimal? TaxRate
);
