using Zorvian.Core.Entities;
using Zorvian.Core.Models;

namespace Zorvian.Application.Interfaces;

public interface IPayrollLocalizationService
{
    Task<decimal> CalculateConceptAsync(PayrollConcept concept, decimal baseSalary, PayrollContext context, Guid employeeId);
}
