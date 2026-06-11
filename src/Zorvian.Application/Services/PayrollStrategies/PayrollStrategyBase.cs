using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.PayrollStrategies;

/// <summary>
/// Base class for country-specific payroll calculation strategies.
/// Implements common tax and social security logic with country-specific overrides.
/// </summary>
public abstract class PayrollCalculationStrategyBase : IPayrollCalculationStrategy
{
    public abstract string CountryCode { get; }

    decimal IPayrollCalculationStrategy.CalculateInssEmployee(decimal grossPay, CountryTaxConfig config) =>
        CalculateINSS(grossPay);

    decimal IPayrollCalculationStrategy.CalculateIr(decimal grossPay, decimal inssEmployee, CountryTaxConfig config)
    {
        var taxableBase = grossPay - inssEmployee;
        return CalculateIR(taxableBase);
    }

    public virtual decimal CalculateINSS(decimal grossSalary)
    {
        // Base implementation: 7% on gross salary
        return Math.Round(grossSalary * 0.07m, 2);
    }

    public virtual decimal CalculateEmployerINSS(decimal grossSalary)
    {
        // Base: 22.5% employer portion
        return Math.Round(grossSalary * 0.225m, 2);
    }

    public virtual decimal CalculateIR(decimal taxableBase)
    {
        // Simplified IR: progressive brackets
        if (taxableBase <= 100000m) return 0m;
        if (taxableBase <= 200000m) return Math.Round(taxableBase * 0.10m, 2);
        if (taxableBase <= 350000m) return Math.Round(taxableBase * 0.15m, 2);
        if (taxableBase <= 500000m) return Math.Round(taxableBase * 0.20m, 2);
        return Math.Round(taxableBase * 0.25m, 2);
    }

    public virtual decimal CalculateNetPay(decimal grossSalary, decimal otherDeductions = 0)
    {
        var inss = CalculateINSS(grossSalary);
        var taxableBase = grossSalary - inss;
        var ir = CalculateIR(taxableBase);
        return grossSalary - inss - ir - otherDeductions;
    }
}

/// <summary>
/// Costa Rica-specific payroll strategy
/// CCSS: 9.34% employee + 26.5% employer
/// LPT (LPT - Impuesto sobre la Renta): 10% - 25% brackets
/// Aguinaldo: 1 month
/// </summary>
public class CostaRicaCalculationStrategy : PayrollCalculationStrategyBase
{
    public override string CountryCode => "CR";

    public override decimal CalculateINSS(decimal grossSalary)
    {
        // Costa Rica SEM/IVM: 9.34% on capped salary (Tope 1,151,000 CRC)
        var capped = Math.Min(grossSalary, 1151000m);
        return Math.Round(capped * 0.0934m, 2);
    }

    public override decimal CalculateEmployerINSS(decimal grossSalary)
    {
        var capped = Math.Min(grossSalary, 1151000m);
        return Math.Round(capped * 0.265m, 2);
    }

    public decimal CalculateAguinaldo(decimal monthlySalary)
    {
        // Costa Rica: 1 month salary
        return monthlySalary;
    }
}

/// <summary>
/// Guatemala-specific payroll strategy
/// IGSS: 4.83% employee + 12.67% employer
/// ISR: 5% - 7% (simplified)
/// Bono 14: 1 month
/// Aguinaldo: 1 month
/// </summary>
public class GuatemalaCalculationStrategy : PayrollCalculationStrategyBase
{
    public override string CountryCode => "GT";

    public override decimal CalculateINSS(decimal grossSalary)
    {
        // Guatemala IGSS: 4.83%
        return Math.Round(grossSalary * 0.0483m, 2);
    }

    public override decimal CalculateEmployerINSS(decimal grossSalary)
    {
        return Math.Round(grossSalary * 0.1267m, 2);
    }

    public override decimal CalculateIR(decimal taxableBase)
    {
        // Guatemala ISR: 5% to 7%
        if (taxableBase <= 30000m) return 0m;
        if (taxableBase <= 60000m) return Math.Round(taxableBase * 0.05m, 2);
        return Math.Round(taxableBase * 0.07m, 2);
    }

    public decimal CalculateBono14(decimal monthlySalary, int monthsWorked)
    {
        // Bono 14: Proportional, paid in July
        return Math.Round((monthlySalary / 12m) * monthsWorked, 2);
    }

    public decimal CalculateAguinaldo(decimal monthlySalary, int monthsWorked)
    {
        return Math.Round((monthlySalary / 12m) * monthsWorked, 2);
    }
}

/// <summary>
/// Honduras-specific payroll strategy
/// IHSS: 5% employee + 10% employer
/// ISR: Progressive 0% - 25%
/// 14avo / 13avo: 1 month each
/// </summary>
public class HondurasCalculationStrategy : PayrollCalculationStrategyBase
{
    public override string CountryCode => "HN";

    public override decimal CalculateINSS(decimal grossSalary)
    {
        // Honduras IHSS: 5%
        return Math.Round(grossSalary * 0.05m, 2);
    }

    public override decimal CalculateEmployerINSS(decimal grossSalary)
    {
        return Math.Round(grossSalary * 0.10m, 2);
    }
}

/// <summary>
/// El Salvador-specific payroll strategy
/// ISSS: 3% employee + 7.5% employer
/// AFP: 7.25% employee + 8.5% employer
/// ISR: Progressive 0% - 30%
/// </summary>
public class ElSalvadorCalculationStrategy : PayrollCalculationStrategyBase
{
    public override string CountryCode => "SV";

    public override decimal CalculateINSS(decimal grossSalary)
    {
        // El Salvador: ISSS 3% + AFP 7.25%
        return Math.Round(grossSalary * 0.03m, 2) + Math.Round(grossSalary * 0.0725m, 2);
    }

    public override decimal CalculateEmployerINSS(decimal grossSalary)
    {
        return Math.Round(grossSalary * 0.075m, 2) + Math.Round(grossSalary * 0.085m, 2);
    }

    public override decimal CalculateIR(decimal taxableBase)
    {
        // El Salvador ISR: 0% to 30%
        if (taxableBase <= 5000m) return 0m;
        if (taxableBase <= 25000m) return Math.Round((taxableBase - 5000) * 0.10m, 2);
        if (taxableBase <= 50000m) return Math.Round(2000m + (taxableBase - 25000) * 0.20m, 2);
        return Math.Round(7000m + (taxableBase - 50000) * 0.30m, 2);
    }
}

/// <summary>
/// Panamá-specific payroll strategy
/// CSS: 9.75% employee + 12.25% employer
/// ISR: Progressive 0% - 27%
/// XIII Mes (Décimo Tercer Mes): 1 month
/// </summary>
public class PanamaCalculationStrategy : PayrollCalculationStrategyBase
{
    public override string CountryCode => "PA";

    public override decimal CalculateINSS(decimal grossSalary)
    {
        // Panamá CSS (Seguro Social): 9.75% employee
        return Math.Round(grossSalary * 0.0975m, 2);
    }

    public override decimal CalculateEmployerINSS(decimal grossSalary)
    {
        // Panamá CSS: 12.25% employer + 1.5% Riesgos Profesionales + 0.56% Educación
        return Math.Round(grossSalary * (0.1225m + 0.015m + 0.0056m), 2);
    }

    public override decimal CalculateIR(decimal taxableBase)
    {
        // Panamá ISR (basado en Ley 8 de 2010)
        if (taxableBase <= 11000m) return 0m;
        if (taxableBase <= 50000m) return Math.Round((taxableBase - 11000) * 0.15m, 2);
        if (taxableBase <= 100000m) return Math.Round((50000 - 11000) * 0.15m + (taxableBase - 50000) * 0.20m, 2);
        return Math.Round((50000 - 11000) * 0.15m + (100000 - 50000) * 0.20m + (taxableBase - 100000) * 0.25m, 2);
    }

    public decimal CalculateDecimoTercerMes(decimal monthlySalary, int monthsWorked)
    {
        // Panamá: XIII Mes (Décimo Tercer Mes) - 1 month per year (paid in 3 installments: Apr, Aug, Dec)
        return Math.Round((monthlySalary / 12m) * monthsWorked, 2);
    }
}