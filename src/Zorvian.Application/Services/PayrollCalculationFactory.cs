using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.PayrollStrategies;

namespace Zorvian.Application.Services;

public sealed class PayrollCalculationFactory
{
    private readonly IEnumerable<IPayrollCalculationStrategy> _strategies;

    public PayrollCalculationFactory(IEnumerable<IPayrollCalculationStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IPayrollCalculationStrategy GetStrategy(string countryCode)
    {
        return _strategies.FirstOrDefault(s => s.CountryCode == countryCode)
            ?? throw new NotSupportedException($"Country code {countryCode} not supported.");
    }
}
