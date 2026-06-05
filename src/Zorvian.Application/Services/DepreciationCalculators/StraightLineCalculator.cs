namespace Zorvian.Application.Services.DepreciationCalculators;

public sealed class StraightLineCalculator : IDepreciationCalculator
{
    public decimal Calculate(decimal acquisitionCost, decimal residualValue, int usefulLifeYears, int currentPeriod,
        decimal? totalUnits, decimal? unitsProduced, decimal accumulatedDepreciation)
    {
        var annualDepreciation = (acquisitionCost - residualValue) / usefulLifeYears;
        return Math.Round(annualDepreciation / 12, 2);
    }
}
