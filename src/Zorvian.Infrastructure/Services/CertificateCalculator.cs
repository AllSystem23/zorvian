using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Calcula variables de certificados accediendo a la BD (Infrastructure).
///
/// La liquidación final es multi-país SIN inventar parámetros legales: las tasas
/// y topes de seguridad social, la tabla progresiva de IR, los días de vacaciones
/// y la fórmula escalonada de indemnización salen de <see cref="Core.Entities.CountryTaxConfig"/>
/// (catálogo global editable por SuperAdmin), y las etiquetas/entidades de
/// <see cref="ContractCountries"/>. La aritmética de prestaciones es la MISMA que
/// usa <see cref="TerminationService"/> para el finiquito, para que el módulo de
/// liquidación y el documento nunca discrepen. Cuando el país no está configurado
/// se cae a Nicaragua (comportamiento histórico).
/// </summary>
public sealed class CertificateCalculator : ICertificateCalculator
{
    private readonly ZorvianDbContext _db;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;

    public CertificateCalculator(ZorvianDbContext db, ICountryTaxConfigRepository taxConfigRepo)
    {
        _db = db;
        _taxConfigRepo = taxConfigRepo;
    }

    public async Task<Dictionary<string, string>> CalculateLiquidationVariablesAsync(
        Guid employeeId, decimal salarioMensual, DateOnly hireDate, DateOnly? terminationDate,
        string? terminationReason, string? countryCode = null)
    {
        var today = DateTime.Now;
        var termDate = terminationDate ?? DateOnly.FromDateTime(today);
        var country = string.IsNullOrWhiteSpace(countryCode) ? "NIC" : countryCode.Trim().ToUpperInvariant();

        // Parámetros del país (catálogo global). País desconocido → Nicaragua.
        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(country);
        var countryData = ContractCountries.Get(country);

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

        // ── Aguinaldo / décimo proporcional ────────────────────────────────
        // Período de cálculo desde la configuración del país (NIC/GTM/CRI/SLV:
        // 1/dic del año anterior; HND: 1/ene). Monto = días del período / 365.
        var aguinaldoStart = TerminationService.ResolveAguinaldoStartDate(hireDate, termDate, taxConfig);
        var aguinaldoDaysWorked = Math.Max(0, termDate.DayNumber - aguinaldoStart.DayNumber + 1);
        var aguinaldoMonto = Math.Round(aguinaldoDaysWorked / 365m * salarioMensual, 2);
        result["periodo_aguinaldo"] = $"{aguinaldoStart:dd/MM/yyyy} - {termDate:dd/MM/yyyy}";
        result["aguinaldo_dias"] = Math.Round(aguinaldoDaysWorked / 365m * 30m, 1).ToString("0.#");
        result["aguinaldo_monto"] = aguinaldoMonto.ToString("N2");

        // ── Vacaciones pendientes ─────────────────────────────────────────
        // Saldo del año si existe; si no, el derecho anual del país (antes 15 fijo).
        var leaveBalance = await _db.LeaveBalances
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == today.Year)
            .FirstOrDefaultAsync();
        var vacDias = leaveBalance?.VacationDaysRemaining ?? taxConfig?.VacationDaysPerYear ?? 15;
        var vacMonto = Math.Round(sueldoDiario * vacDias, 2);
        result["periodo_vacaciones"] = today.Year.ToString();
        result["vacaciones_dias"] = vacDias.ToString("0.##");
        result["vacaciones_monto"] = vacMonto.ToString("N2");

        // ── Indemnización por antigüedad ──────────────────────────────────
        // Fórmula escalonada del país (NIC Art. 45: 30 d/año 1-3, 20 d/año 4-6,
        // tope 150 días) o plana (días/año × años, tope en años) si no hay escalones.
        var indemDias = TerminationService.CalculateSeveranceDays(years, taxConfig);
        var indemMonto = Math.Round(sueldoDiario * indemDias, 2);
        result["periodo_indem"] = $"{hireDate:dd/MM/yyyy} - {termDate:dd/MM/yyyy}";
        result["indemnizacion_dias"] = indemDias.ToString("0.##");
        result["indemnizacion_monto"] = indemMonto.ToString("N2");

        // ── Deducciones del trabajador (configuración del país) ───────────
        var inssEmployeeRate = taxConfig?.InssEmployeeRate ?? 0.07m;
        var inssEmployeeMax = taxConfig?.InssEmployeeMax ?? 0m;
        var inssLaboral = Math.Round(salarioMensual * inssEmployeeRate, 2);
        if (inssEmployeeMax > 0)
            inssLaboral = Math.Min(inssLaboral, inssEmployeeMax);

        // IR sobre salario ordinario: tabla progresiva del país proyectada a 12 meses
        // (misma función que el finiquito).
        var irOrdinario = Math.Round(TerminationService.CalculateIrOnMonthlySalary(salarioMensual, taxConfig), 2);

        // ── Aportes patronales de referencia (configuración del país) ─────
        var inssEmployerRate = taxConfig?.InssEmployerRate ?? 0.225m;
        var inssPatronal = Math.Round(salarioMensual * inssEmployerRate, 2);
        var otherEmployerRate = taxConfig?.OtherEmployerRate ?? 0.02m;
        var otherEmployerMonto = Math.Round(salarioMensual * otherEmployerRate, 2);

        result["inss_laboral"] = inssLaboral.ToString("N2");
        result["ir_ord"] = irOrdinario.ToString("N2");
        result["ir_ocasional"] = "0.00";
        result["ir_indemnizacion"] = "0.00";
        result["inss_patronal"] = inssPatronal.ToString("N2");
        // `inatec` se conserva como alias histórico (plantillas de usuario que lo usen).
        result["inatec"] = otherEmployerMonto.ToString("N2");
        result["other_employer_monto"] = otherEmployerMonto.ToString("N2");
        // País sin configuración → respaldo Nicaragua (INATEC, como el documento histórico);
        // país configurado sin otro aporte (p. ej. Costa Rica) → nombre vacío y fila oculta.
        result["other_employer_name"] = taxConfig is null
            ? "INATEC"
            : taxConfig.OtherEmployerName ?? "";
        result["inss_tasa"] = Percent(inssEmployeeRate);
        result["inss_patronal_tasa"] = Percent(inssEmployerRate);
        result["other_employer_tasa"] = Percent(otherEmployerRate);

        // Totales
        var totalIngresos = salarioMensual + aguinaldoMonto + vacMonto + indemMonto;
        var totalDeducciones = inssLaboral + irOrdinario;
        var neto = totalIngresos - totalDeducciones;

        result["total_ingresos"] = totalIngresos.ToString("N2");
        result["total_deducciones"] = totalDeducciones.ToString("N2");
        result["neto_a_recibir"] = neto.ToString("N2");
        result["neto_en_letras"] = NumeroALetras(neto, countryData.CurrencyName);

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

    /// <summary>Tasa decimal → etiqueta porcentual de la plantilla ("7.00%").</summary>
    private static string Percent(decimal rate) => (rate * 100m).ToString("N2") + "%";

    private static string NumeroALetras(decimal numero, string? currencyPlural)
    {
        var moneda = string.IsNullOrWhiteSpace(currencyPlural) ? "córdobas" : currencyPlural.Trim();

        if (numero == 0) return $"Cero {moneda} con 00/100";

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

        return $"{string.Join(" ", partes)} {moneda} con {decimales:00}/100";
    }

    private static string Unidades(long numero)
    {
        if (numero < 100) return DecenasOUnidades(numero);

        var centenas = numero / 100;
        var resto = numero % 100;

        var nombreCentena = centenas switch
        {
            1 => numero == 100 ? "Cien" : "Ciento",
            2 => "Doscientos",
            3 => "Trescientos",
            4 => "Cuatrocientos",
            5 => "Quinientos",
            6 => "Seiscientos",
            7 => "Setecientos",
            8 => "Ochocientos",
            9 => "Novecientos",
            _ => "",
        };

        return resto == 0 ? nombreCentena : $"{nombreCentena} {DecenasOUnidades(resto)}";
    }

    private static string DecenasOUnidades(long numero)
    {
        var unidades = new[]
        {
            "", "Uno", "Dos", "Tres", "Cuatro", "Cinco", "Seis", "Siete", "Ocho", "Nueve",
            "Diez", "Once", "Doce", "Trece", "Catorce", "Quince", "Dieciséis", "Diecisiete",
            "Dieciocho", "Diecinueve", "Veinte"
        };

        if (numero <= 20) return unidades[numero];
        if (numero < 30) return $"Veinti{unidades[numero - 20].ToLowerInvariant()}";

        var decenas = new[] { "", "", "Veinte", "Treinta", "Cuarenta", "Cincuenta", "Sesenta", "Setenta", "Ochenta", "Noventa" };
        var decena = numero / 10;
        var resto = numero % 10;

        return resto == 0 ? decenas[decena] : $"{decenas[decena]} y {unidades[resto].ToLowerInvariant()}";
    }
}
