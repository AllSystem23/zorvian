using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Calcula variables de certificados accediendo a la BD (Infrastructure).
/// </summary>
public sealed class CertificateCalculator : ICertificateCalculator
{
    private readonly ZorvianDbContext _db;

    public CertificateCalculator(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, string>> CalculateLiquidationVariablesAsync(
        Guid employeeId, decimal salarioMensual, DateOnly hireDate, DateOnly? terminationDate, string? terminationReason)
    {
        var today = DateTime.Now;
        var termDate = terminationDate ?? DateOnly.FromDateTime(today);

        // Antigüedad
        var years = termDate.Year - hireDate.Year;
        var months = termDate.Month - hireDate.Month;
        if (months < 0) { years--; months += 12; }
        var days = termDate.Day - hireDate.Day;
        if (days < 0) { months--; if (months < 0) { years--; months += 12; } days += 30; }

        var result = new Dictionary<string, string>();

        result["seniority"] = $"{years} años, {months} meses, {days} días";
        result["contract_type"] = "Tiempo Indefinido";
        result["retiro_reason"] = terminationReason ?? "Voluntario";
        result["fecha_elaboracion"] = today.ToString("dd/MM/yyyy");
        result["fecha_ingreso"] = hireDate.ToString("dd/MM/yyyy");
        result["fecha_salida"] = termDate.ToString("dd/MM/yyyy");

        // Salarios derivados
        var sueldoDiario = Math.Round(salarioMensual / 30, 2);
        var sueldoPorHora = Math.Round(salarioMensual / 30 / 8, 2);
        var horaExtraValor = Math.Round(sueldoPorHora * 2, 2);

        result["salario_mensual"] = salarioMensual.ToString("N2");
        result["sueldo_diario"] = sueldoDiario.ToString("N2");
        result["sueldo_por_hora"] = sueldoPorHora.ToString("N2");
        result["hora_extra_valor"] = horaExtraValor.ToString("N2");

        // Salario promedio y más alto (últimos 6 meses)
        var salaries = await _db.EmployeeSalaries
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .OrderByDescending(s => s.EffectiveDate)
            .Take(6)
            .ToListAsync();

        if (salaries.Count > 0)
        {
            result["salario_promedio"] = salaries.Average(s => s.BaseSalary).ToString("N2");
            result["salario_mas_alto"] = salaries.Max(s => s.BaseSalary).ToString("N2");
        }
        else
        {
            result["salario_promedio"] = salarioMensual.ToString("N2");
            result["salario_mas_alto"] = salarioMensual.ToString("N2");
        }

        // Aguinaldo proporcional
        var aguinaldoMonto = Math.Round(salarioMensual / 12 * months + salarioMensual / 12 / 30 * days, 2);
        result["periodo_aguinaldo"] = $"{hireDate:dd/MM/yyyy} - {termDate:dd/MM/yyyy}";
        result["aguinaldo_dias"] = "15";
        result["aguinaldo_monto"] = aguinaldoMonto.ToString("N2");

        // Vacaciones pendientes
        var leaveBalance = await _db.LeaveBalances
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == today.Year)
            .FirstOrDefaultAsync();
        var vacDias = leaveBalance?.VacationDaysRemaining ?? 15;
        var vacMonto = Math.Round(sueldoDiario * vacDias, 2);
        result["periodo_vacaciones"] = today.Year.ToString();
        result["vacaciones_dias"] = vacDias.ToString("0.##");
        result["vacaciones_monto"] = vacMonto.ToString("N2");

        // Indemnización por antigüedad
        var indemMonto = Math.Round(salarioMensual * years + salarioMensual / 12 * months, 2);
        result["periodo_indem"] = $"{hireDate:dd/MM/yyyy} - {termDate:dd/MM/yyyy}";
        result["indemnizacion_monto"] = indemMonto.ToString("N2");

        // Deducciones
        var inssLaboral = Math.Round(salarioMensual * 0.07m, 2);
        var irOrdinario = Math.Round(CalcularIr(salarioMensual), 2);
        var inssPatronal = Math.Round(salarioMensual * 0.215m, 2);
        var inatec = Math.Round(salarioMensual * 0.02m, 2);

        result["inss_laboral"] = inssLaboral.ToString("N2");
        result["ir_ord"] = irOrdinario.ToString("N2");
        result["ir_ocasional"] = "0.00";
        result["ir_indemnizacion"] = "0.00";
        result["inss_patronal"] = inssPatronal.ToString("N2");
        result["inatec"] = inatec.ToString("N2");

        // Totales
        var totalIngresos = salarioMensual + aguinaldoMonto + vacMonto + indemMonto;
        var totalDeducciones = inssLaboral + irOrdinario;
        var neto = totalIngresos - totalDeducciones;

        result["total_ingresos"] = totalIngresos.ToString("N2");
        result["total_deducciones"] = totalDeducciones.ToString("N2");
        result["neto_a_recibir"] = neto.ToString("N2");
        result["neto_en_letras"] = NumeroALetras(neto);

        result["elaborado_por"] = "Departamento de Nómina";
        result["revisado_por"] = "Departamento de RRHH";
        result["autorizado_por"] = "";

        return result;
    }

    public Dictionary<string, string> CalculatePowerOfAttorneyVariables(
        string companyName, string? companyTaxId, string employeeName, string? employeeIdNumber)
    {
        return new Dictionary<string, string>
        {
            ["grantor_name"] = companyName,
            ["grantor_id"] = companyTaxId ?? "",
            ["attorney_name"] = employeeName,
            ["attorney_id"] = employeeIdNumber ?? "",
            ["powers"] = "Representar legalmente a la empresa en trámites administrativos, notariales y judiciales; firmar documentos legales; y realizar gestiones en nombre de la compañía.",
            ["scope"] = "El presente poder tendrá vigencia hasta nueva notificación."
        };
    }

    private static decimal CalcularIr(decimal salarioMensual)
    {
        var salarioAnual = salarioMensual * 12;
        var baseImponible = Math.Max(0, salarioAnual - 2024m * 365m);

        if (baseImponible <= 100000m) return 0;
        if (baseImponible <= 200000m) return (baseImponible - 100000m) * 0.10m;
        if (baseImponible <= 350000m) return 10000m + (baseImponible - 200000m) * 0.15m;
        if (baseImponible <= 500000m) return 32500m + (baseImponible - 350000m) * 0.20m;
        return 62500m + (baseImponible - 500000m) * 0.25m;
    }

    private static string NumeroALetras(decimal numero)
    {
        if (numero == 0) return "Cero córdobas con 00/100";

        var entero = (long)numero;
        var decimales = (int)((numero - entero) * 100);
        var partes = new List<string>();

        if (entero >= 1000000)
        {
            var millones = entero / 1000000;
            partes.Add(millones == 1 ? "Un millón" : $"{Unidades(millones)} millones");
            entero %= 1000000;
        }
        if (entero >= 1000)
        {
            var miles = entero / 1000;
            partes.Add(miles == 1 ? "Mil" : $"{Unidades(miles)} mil");
            entero %= 1000;
        }
        if (entero > 0) partes.Add(Unidades(entero));

        return $"{string.Join(" ", partes)} córdobas con {decimales:00}/100";
    }

    private static string Unidades(long numero)
    {
        var unidades = new[]
        {
            "", "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve",
            "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", "Dieciséis", "Diecisiete",
            "Dieciocho", "Diecinueve", "Veinte"
        };

        if (numero <= 20) return unidades[numero];
        if (numero < 30) return $"Veinti{unidades[numero - 20]}";

        var decenas = new[] { "", "", "Veinte", "Treinta", "Cuarenta", "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" };
        var decena = numero / 10;
        var resto = numero % 10;

        return resto == 0 ? decenas[decena] : $"{decenas[decena]} y {unidades[resto]}";
    }
}
