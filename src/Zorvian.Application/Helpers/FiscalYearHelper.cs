namespace Zorvian.Application.Helpers;

/// <summary>
/// Pure utility for fiscal year date calculations.
/// Fiscal month config lives in CountryTaxConfig.DefaultFiscalStartMonth (database).
/// Contains only a minimal ISO country code mapping (standard lookup, not business logic).
/// </summary>
public static class FiscalYearHelper
{
    /// <summary>
    /// Maps a country name to its ISO-3166 alpha-3 code.
    /// Used to look up CountryTaxConfig from the database.
    /// </summary>
    public static string MapCountryToCode(string? country) => (country ?? "").ToLowerInvariant() switch
    {
        "nicaragua" => "NIC",
        "costa rica" => "CRI",
        "honduras" => "HND",
        "el salvador" or "salvador" => "SLV",
        "guatemala" => "GTM",
        "panamá" or "panama" => "PAN",
        "uk" or "united kingdom" or "reino unido" => "GBR",
        "india" => "IND",
        "usa" or "united states" or "estados unidos" or "eeuu" => "USA",
        _ => "NIC",
    };
    /// <summary>
    /// Calculates the fiscal year start date given a year and start month.
    /// </summary>
    public static DateOnly GetStartDate(int year, int startMonth)
        => new(year, Math.Clamp(startMonth, 1, 12), 1);

    /// <summary>
    /// Calculates the fiscal year end date given a year and start month.
    /// For calendar year (month 1): Dec 31 of same year.
    /// For mid-year starts (e.g. month 4): last day of month before start in the next year.
    /// </summary>
    public static DateOnly GetEndDate(int year, int startMonth)
    {
        var month = Math.Clamp(startMonth, 1, 12);
        return month == 1
            ? new DateOnly(year, 12, 31)
            : new DateOnly(year + 1, month - 1, 1).AddDays(-1);
    }

    /// <summary>
    /// Gets the human-readable label for a fiscal year configuration.
    /// </summary>
    public static string GetFiscalYearLabel(int startMonth)
    {
        return startMonth switch
        {
            1 => "Año calendario (Ene - Dic)",
            4 => "Abril - Marzo",
            7 => "Julio - Junio",
            9 => "Septiembre - Agosto",
            _ => $"Mes {startMonth} - Mes {startMonth - 1}"
        };
    }

    /// <summary>
    /// Resolves fiscal year dates using the company settings' configurable month
    /// as the primary source, falling back to the country default from the database.
    /// Priority: CompanySettings.FiscalYearStartMonth > CountryTaxConfig.DefaultFiscalStartMonth > 1
    /// </summary>
    public static (DateOnly StartDate, DateOnly EndDate, int EffectiveMonth) ResolveFiscalYearDates(
        int year, int? configurableStartMonth, int? countryDefaultStartMonth)
    {
        var effectiveMonth = configurableStartMonth > 0
            ? configurableStartMonth.Value
            : countryDefaultStartMonth > 0
                ? countryDefaultStartMonth.Value
                : 1; // ultimate fallback: calendar year

        return (GetStartDate(year, effectiveMonth), GetEndDate(year, effectiveMonth), effectiveMonth);
    }

    /// <summary>
    /// Gets fiscal year start/end dates by country name.
    /// Uses CountryTaxConfig.DefaultFiscalStartMonth when available, falls back to known defaults.
    /// </summary>
    public static (DateOnly StartDate, DateOnly EndDate, int EffectiveMonth) GetFiscalYearDatesByCountry(int year, string countryName)
    {
        var countryCode = MapCountryToCode(countryName);
        var effectiveMonth = GetDefaultFiscalStartMonth(countryCode);
        return (GetStartDate(year, effectiveMonth), GetEndDate(year, effectiveMonth), effectiveMonth);
    }

    /// <summary>
    /// Returns the default fiscal start month for a given ISO country code.
    /// This is a built-in lookup for common countries; CountryTaxConfig in DB takes precedence.
    /// </summary>
    public static int GetDefaultFiscalStartMonth(string countryCode) => countryCode.ToUpperInvariant() switch
    {
        "NIC" or "CRI" or "HND" or "SLV" or "GTM" or "PAN" => 1,  // Central America: Calendar year
        "GBR" => 4,   // UK: April
        "IND" => 4,    // India: April
        "USA" => 1,   // USA: Calendar year (configurable by company)
        _ => 1          // Default: Calendar year
    };
}
