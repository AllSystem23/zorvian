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
    string InssRegime
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
    string? InssRegime
);

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
