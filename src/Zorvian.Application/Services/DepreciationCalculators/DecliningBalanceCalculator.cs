namespace Zorvian.Application.Services.DepreciationCalculators;

public sealed class DecliningBalanceCalculator : IDepreciationCalculator
{
    public decimal Calculate(decimal acquisitionCost, decimal residualValue, int usefulLifeYears, int currentPeriod,
        decimal? totalUnits, decimal? unitsProduced, decimal accumulatedDepreciation)
    {
        var netBookValue = acquisitionCost - accumulatedDepreciation;
        var rate = 2.0m / usefulLifeYears;
        var annualDepreciation = netBookValue * rate;
        var maxDepreciable = acquisitionCost - residualValue;
        var remaining = maxDepreciable - accumulatedDepreciation;
        var monthlyAmount = Math.Round(annualDepreciation / 12, 2);
        return Math.Min(monthlyAmount, Math.Max(0, remaining));
    }
}
