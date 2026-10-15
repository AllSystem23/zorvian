namespace Zorvian.Application.Interfaces;

/// <summary>
/// Calcula las variables específicas para cada tipo de certificado.
/// Implementado en Infrastructure para acceder a la BD.
/// </summary>
public interface ICertificateCalculator
{
    Task<Dictionary<string, string>> CalculateLiquidationVariablesAsync(
        Guid employeeId, decimal salarioMensual, DateOnly hireDate, DateOnly? terminationDate, string? terminationReason);

    Dictionary<string, string> CalculatePowerOfAttorneyVariables(
        string companyName, string? companyTaxId, string employeeName, string? employeeIdNumber);
}
