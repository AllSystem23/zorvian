namespace Zorvian.Application.Helpers;

/// <summary>
/// Datos laborales y fiscales por país para el Contrato Individual de Trabajo.
/// Fuentes: legislación laboral vigente de cada país (Código del Trabajo) y
/// entidades tributarias oficiales. Los códigos de país son ISO-3166 alpha-3,
/// los mismos que usa ElectronicInvoiceService y FleetCatalogPage.
/// </summary>
public sealed record ContractCountryData(
    string CountryCode,
    string CountryName,
    string CurrencyCode,
    string CurrencySymbol,
    string CurrencyName,
    int WeeklyHours,
    string WeeklySchedule,
    int ProbationMonths,
    int VacationDays,
    string TaxIdName,
    string IdDocumentName,
    string LaborLawName,
    string TaxAuthorityName,
    string SocialSecurityName,
    string ThirteenthMonthName,
    string VatLabel);

public static class ContractCountries
{
    public static ContractCountryData Get(string? countryCode) => (countryCode ?? "").ToUpperInvariant() switch
    {
        "CRI" => new(
            "CRI", "Costa Rica", "CRC", "₡", "colones costarricenses",
            48, "de lunes a viernes", 2, 14,
            "Cédula Jurídica", "Cédula de Identidad",
            "Código de Trabajo de la República de Costa Rica", "Ministerio de Hacienda",
            "Caja Costarricense de Seguro Social (CCSS)", "Aguinaldo", "IV (13%)"),
        "GTM" => new(
            "GTM", "Guatemala", "GTQ", "Q", "quetzales",
            44, "de lunes a viernes", 2, 15,
            "NIT", "Documento Personal de Identificación (DPI)",
            "Código de Trabajo de la República de Guatemala", "Superintendencia de Administración Tributaria (SAT)",
            "Instituto Guatemalteco de Seguridad Social (IGSS)", "Bono 14 y Aguinaldo", "IVA (15%)"),
        "HND" => new(
            "HND", "Honduras", "HNL", "L", "lempiras",
            44, "de lunes a viernes", 2, 10,
            "RTN", "Tarjeta de Identificación",
            "Código del Trabajo de la República de Honduras", "Servicio de Administración de Rentas (SAR)",
            "Instituto Hondureño de Seguridad Social (IHSS)", "Décimo Cuarto Mes (Aguinaldo)", "ISV (15%)"),
        "SLV" => new(
            "SLV", "El Salvador", "USD", "$", "dólares estadounidenses",
            44, "de lunes a viernes", 1, 15,
            "NIT", "Documento Único de Identidad (DUI)",
            "Código de Trabajo de la República de El Salvador", "Ministerio de Hacienda",
            "Instituto Salvadoreño del Seguro Social (ISSS)", "Aguinaldo", "IVA (13%)"),
        "PAN" => new(
            "PAN", "Panamá", "PAB", "B/.", "balboas",
            48, "de lunes a sábado", 3, 30,
            "RUC", "Cédula de Identidad",
            "Código de Trabajo de la República de Panamá", "Dirección General de Ingresos (DGI)",
            "Caja de Seguro Social (CSS)", "Catorceno Mes (13° mes)", "ITBMS (7%)"),
        _ => new(
            "NIC", "Nicaragua", "NIO", "C$", "córdobas",
            48, "de lunes a sábado", 3, 15,
            "RUC", "Cédula de Identidad",
            "Código del Trabajo de la República de Nicaragua", "Dirección General de Ingresos (DGI)",
            "Instituto Nicaragüense de Seguridad Social (INSS)", "Aguinaldo (mes adicional en diciembre)", "IVA (15%)"),
    };
}
