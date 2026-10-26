namespace Zorvian.Application.DTOs.Company;

public sealed record CreateCompanyRequest(
    string Name,
    string LegalName,
    string? TaxId,
    string? Phone,
    string? Address,
    string? Email,
    string Country,
    string Currency,
    string Timezone,
    int MaxEmployees = 50
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
    string Country,
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
    string? Email,
    string? Country,
    string? Currency,
    string? Timezone,
    string? LogoUrl,
    int? MaxEmployees,
    bool? IsActive,
    string? SubscriptionPlan
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
    decimal TaxRate,
    int FiscalYearStartMonth,
    string InssRegime,
    string RetentionBase
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
    decimal? TaxRate,
    int? FiscalYearStartMonth,
    string? InssRegime,
    string? RetentionBase
);

/// <summary>
/// Logo de la compañía en contexto. <paramref name="LogoUrl"/> ya viene firmada
/// (TTL corto): no debe cachearse, se vuelve a pedir al abrir la pantalla.
/// </summary>
public sealed record CompanyLogoResponse(Guid Id, string? LogoUrl);

public sealed record CompanyListItemResponse(
    Guid Id,
    string TenantId,
    string Name,
    string LegalName,
    string? TaxId,
    string? LogoUrl,
    string? Email,
    string? Phone,
    string Country,
    string Currency,
    string Timezone,
    bool IsActive,
    string SubscriptionPlan,
    int MaxEmployees,
    DateTime CreatedAt
);
