using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPayrollCalculationStrategy
{
    string CountryCode { get; }
    decimal CalculateInssEmployee(decimal grossPay, CountryTaxConfig config);
    decimal CalculateIr(decimal grossPay, decimal inssEmployee, CountryTaxConfig config);
}
