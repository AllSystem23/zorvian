namespace Zorvian.Application.DTOs.Tax;

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
    bool IsActive
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
    bool HasFourteenthMonth = false
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
    bool? IsActive
);
