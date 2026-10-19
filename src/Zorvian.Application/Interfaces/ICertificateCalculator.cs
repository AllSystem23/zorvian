namespace Zorvian.Application.Interfaces;

/// <summary>
/// Calcula las variables específicas para cada tipo de certificado.
/// Implementado en Infrastructure para acceder a la BD.
/// </summary>
public interface ICertificateCalculator
{
    /// <summary>
    /// Variables de la liquidación final. <c>countryCode</c> es el ISO-3166 alpha-3 de la
    /// compañía (NIC/CRI/GTM/HND/SLV/PAN): las tasas, topes, tabla de IR y días de
    /// prestaciones salen de <c>CountryTaxConfig</c>; null/desconocido cae a Nicaragua.
    /// </summary>
    Task<Dictionary<string, string>> CalculateLiquidationVariablesAsync(
        Guid employeeId, decimal salarioMensual, DateOnly hireDate, DateOnly? terminationDate,
        string? terminationReason, string? countryCode = null);

    Dictionary<string, string> CalculatePowerOfAttorneyVariables(
        string companyName, string? companyTaxId, string employeeName, string? employeeIdNumber);
}
