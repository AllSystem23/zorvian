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
        var strategy = _strategies.FirstOrDefault(s =>
            s.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase));

        if (strategy is not null)
            return strategy;

        var supported = string.Join(", ", _strategies.Select(s => s.CountryCode));
        throw new NotSupportedException(
            $"No payroll strategy found for country code '{countryCode}'. " +
            $"Supported codes: {supported}. " +
            $"Verify the Company.Country field or register a new IPayrollCalculationStrategy.");
    }
}
