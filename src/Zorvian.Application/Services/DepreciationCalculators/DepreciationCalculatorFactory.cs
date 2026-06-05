namespace Zorvian.Application.Services.DepreciationCalculators;

public sealed class DepreciationCalculatorFactory
{
    private readonly Dictionary<string, IDepreciationCalculator> _calculators;

    public DepreciationCalculatorFactory()
    {
        _calculators = new Dictionary<string, IDepreciationCalculator>
        {
            ["StraightLine"] = new StraightLineCalculator(),
            ["DecliningBalance"] = new DecliningBalanceCalculator(),
            ["SumOfYearsDigits"] = new SumOfYearsDigitsCalculator(),
            ["UnitsOfProduction"] = new UnitsOfProductionCalculator(),
        };
    }

    public IDepreciationCalculator GetCalculator(string method)
    {
        return _calculators.TryGetValue(method, out var calculator)
            ? calculator
            : _calculators["StraightLine"];
    }
}
