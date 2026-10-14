using Zorvian.Core.Entities;

namespace Zorvian.Application.Helpers;

/// <summary>
/// Contexto base de documento: claves Company.* / Country.* / Contract.City compartidas
/// por TODAS las plantillas (contrato, oferta, constancia, cotización, nota, poder).
/// Se inyecta como base en DocumentService (generación y preview) para que el path del
/// wizard manual también tenga datos de compañía/país, y lo reutiliza
/// DocumentGenerationService para los contextos de entidad.
/// </summary>
public static class DocumentCompanyContext
{
    public static Dictionary<string, string> Build(Company? company)
    {
        var data = new Dictionary<string, string>();
        var companyName = company?.Name ?? "Mi Empresa";

        data["Company.Name"] = companyName;
        data["Company.LegalName"] = string.IsNullOrEmpty(company?.LegalName) ? companyName : company.LegalName;
        data["Company.Date"] = DateTime.UtcNow.ToString("dd/MM/yyyy");
        data["Company.TaxId"] = company?.TaxId ?? "";
        data["Company.Address"] = company?.Address ?? "";
        data["Company.CountryCode"] = FiscalYearHelper.MapCountryToCode(company?.Country);
        data["Company.Currency"] = string.IsNullOrEmpty(company?.Currency) ? "NIO" : company.Currency;

        var country = ContractCountries.Get(data["Company.CountryCode"]);
        data["Country.Code"] = country.CountryCode;
        data["Country.Name"] = country.CountryName;
        data["Country.Currency"] = country.CurrencySymbol;
        data["Country.CurrencyCode"] = country.CurrencyCode;
        data["Country.CurrencyName"] = country.CurrencyName;
        data["Country.WeeklyHours"] = country.WeeklyHours.ToString();
        data["Country.WeeklySchedule"] = country.WeeklySchedule;
        data["Country.ProbationMonths"] = country.ProbationMonths.ToString();
        data["Country.VacationDays"] = country.VacationDays.ToString();
        data["Country.IdDocument"] = country.IdDocumentName;
        data["Country.LaborLaw"] = country.LaborLawName;
        data["Country.TaxAuthority"] = country.TaxAuthorityName;
        data["Country.SocialSecurity"] = country.SocialSecurityName;
        data["Country.ThirteenthMonth"] = country.ThirteenthMonthName;
        data["Country.VatLabel"] = country.VatLabel;
        data["Company.TaxIdLabel"] = country.TaxIdName;

        // Lugar de emisión: domicilio de la compañía (de allí "sale" el documento).
        data["Contract.City"] = company?.Address ?? "";

        return data;
    }
}
