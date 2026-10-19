namespace Zorvian.Application.DTOs.Tax;

/// <summary>
/// Configuración fiscal de un país. Es un catálogo GLOBAL (SuperAdmin) que alimenta
/// la nómina, la liquidación final y los documentos: las tasas y los topes que se
/// editen aquí son los que el sistema usa por defecto.
/// </summary>
public sealed record CountryTaxConfigResponse(
    Guid Id,
    string CountryCode,
    string CountryName,
    string Currency,
    decimal InssEmployeeRate,
    decimal InssEmployeeMax,
    decimal InssEmployerRate,
    decimal InssEmployerMax,
    decimal OtherEmployerRate,
    string? OtherEmployerName,
    decimal IrExemptAmount,
    string IrTableJson,
    int VacationDaysPerYear,
    decimal ChristmasBonusPercentage,
    int IndemnityDaysPerYear,
    int MaxIndemnityYears,
    bool HasThirteenthMonth,
    bool HasFourteenthMonth,
    bool IsActive,
    // ── Regímenes INSS (Decreto 06-2019 en Nicaragua) ──
    decimal InssIntegralEmployeeRate,
    decimal InssIntegralEmployerRate,
    decimal InssIntegralEmployerRateSmall,
    decimal InssIvmEmployeeRate,
    decimal InssIvmEmployerRate,
    decimal InssIvmEmployerRateSmall,
    int InssSmallEmployerThreshold,
    // ── Prestaciones ──
    string IndemnityTiersJson,
    int MaxIndemnityDays,
    int AguinaldoPeriodStartMonth,
    int AguinaldoPeriodStartDay,
    int DefaultFiscalStartMonth
);

/// <summary>
/// Configuración fiscal del país de LA COMPAÑÍA (ajuste propio sobre el catálogo
/// global). <c>IsCustomized = false</c> = hoy se está usando el catálogo tal cual;
/// <c>Config = null</c> = la compañía no tiene país configurado.
/// </summary>
public sealed record MyCountryTaxConfigResponse(
    string CountryCode,
    string CountryName,
    bool IsCustomized,
    CountryTaxConfigResponse? Config
);

public sealed record CreateCountryTaxConfigRequest(
    string CountryCode,
    string CountryName,
    string Currency,
    decimal InssEmployeeRate = 0,
    decimal InssEmployeeMax = 0,
    decimal InssEmployerRate = 0,
    decimal InssEmployerMax = 0,
    decimal OtherEmployerRate = 0,
    string? OtherEmployerName = null,
    decimal IrExemptAmount = 0,
    string IrTableJson = "[]",
    int VacationDaysPerYear = 15,
    decimal ChristmasBonusPercentage = 0,
    int IndemnityDaysPerYear = 0,
    int MaxIndemnityYears = 0,
    bool HasThirteenthMonth = false,
    bool HasFourteenthMonth = false,
    // ── Regímenes INSS ──
    decimal InssIntegralEmployeeRate = 0,
    decimal InssIntegralEmployerRate = 0,
    decimal InssIntegralEmployerRateSmall = 0,
    decimal InssIvmEmployeeRate = 0,
    decimal InssIvmEmployerRate = 0,
    decimal InssIvmEmployerRateSmall = 0,
    int InssSmallEmployerThreshold = 50,
    // ── Prestaciones ──
    string IndemnityTiersJson = "[]",
    int MaxIndemnityDays = 0,
    int AguinaldoPeriodStartMonth = 12,
    int AguinaldoPeriodStartDay = 1,
    int DefaultFiscalStartMonth = 1
);

public sealed record UpdateCountryTaxConfigRequest(
    string? CountryCode,
    string? CountryName,
    string? Currency,
    decimal? InssEmployeeRate,
    decimal? InssEmployeeMax,
    decimal? InssEmployerRate,
    decimal? InssEmployerMax,
    decimal? OtherEmployerRate,
    string? OtherEmployerName,
    decimal? IrExemptAmount,
    string? IrTableJson,
    int? VacationDaysPerYear,
    decimal? ChristmasBonusPercentage,
    int? IndemnityDaysPerYear,
    int? MaxIndemnityYears,
    bool? HasThirteenthMonth,
    bool? HasFourteenthMonth,
    bool? IsActive,
    // ── Regímenes INSS ──
    decimal? InssIntegralEmployeeRate = null,
    decimal? InssIntegralEmployerRate = null,
    decimal? InssIntegralEmployerRateSmall = null,
    decimal? InssIvmEmployeeRate = null,
    decimal? InssIvmEmployerRate = null,
    decimal? InssIvmEmployerRateSmall = null,
    int? InssSmallEmployerThreshold = null,
    // ── Prestaciones ──
    string? IndemnityTiersJson = null,
    int? MaxIndemnityDays = null,
    int? AguinaldoPeriodStartMonth = null,
    int? AguinaldoPeriodStartDay = null,
    int? DefaultFiscalStartMonth = null
);
