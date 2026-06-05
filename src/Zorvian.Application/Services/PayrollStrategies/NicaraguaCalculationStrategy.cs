using System.Text.Json;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.PayrollStrategies;

public sealed class NicaraguaCalculationStrategy : IPayrollCalculationStrategy
{
    public string CountryCode => "NIC";

    public decimal CalculateInssEmployee(decimal grossPay, CountryTaxConfig config)
    {
        var inss = grossPay * config.InssEmployeeRate;
        return config.InssEmployeeMax > 0 ? Math.Min(inss, config.InssEmployeeMax) : inss;
    }

    public decimal CalculateIr(decimal grossPay, decimal inssEmployee, CountryTaxConfig config)
    {
        var taxableIncome = grossPay - inssEmployee;
        var brackets = JsonSerializer.Deserialize<List<IrBracket>>(config.IrTableJson ?? "[]") 
                       ?? new List<IrBracket>();
        
        var bracket = brackets.FirstOrDefault(b => taxableIncome >= b.Min && taxableIncome <= b.Max)
                      ?? brackets.LastOrDefault(b => taxableIncome > b.Max)
                      ?? brackets.FirstOrDefault();

        if (bracket == null) return 0m;
        
        return bracket.FixedAmount + (taxableIncome - bracket.Min) * bracket.Rate;
    }

    private record IrBracket(decimal Min, decimal Max, decimal Rate, decimal FixedAmount);
}
