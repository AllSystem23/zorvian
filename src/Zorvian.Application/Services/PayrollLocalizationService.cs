using System.Data;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Models;

namespace Zorvian.Application.Services;

public sealed class PayrollLocalizationService : IPayrollLocalizationService
{
    private readonly IEmployeePayrollExemptionRepository _exemptionRepository;

    public PayrollLocalizationService(IEmployeePayrollExemptionRepository exemptionRepository)
    {
        _exemptionRepository = exemptionRepository;
    }

    public async Task<decimal> CalculateConceptAsync(PayrollConcept concept, decimal baseSalary, PayrollContext context, Guid employeeId)
    {
        try
        {
            // 1. Check for Exemptions
            var isExempt = await _exemptionRepository.IsExemptAsync(employeeId, concept.Id);

            if (isExempt) return 0m;

            var formula = concept.CalculationFormula;

            // 2. Dynamic Rate Logic (Example: INSS Patronal)
            if (concept.Code == "INSS_PAT")
            {
                var rate = context.EmployeeCount >= 50 ? 0.225m : 0.215m;
                formula = formula.Replace("Salary * 0.225", $"Salary * {rate}");
            }

            // 3. Liquidación Logic (Context-based)
            if (context.Termination != null)
            {
                if (concept.Code == "INDEM_ANT" && context.Termination.Type == Zorvian.Core.Entities.TerminationReason.VoluntaryResignation)
                    return 0m; 
            }

            formula = formula.Replace("Salary", baseSalary.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
            
            var table = new DataTable();
            var result = table.Compute(formula, string.Empty);
            
            return Convert.ToDecimal(result);
        }
        catch
        {
            return 0m;
        }
    }
}
