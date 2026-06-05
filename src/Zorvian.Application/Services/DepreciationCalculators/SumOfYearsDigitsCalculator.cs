namespace Zorvian.Application.Services.DepreciationCalculators;

public sealed class SumOfYearsDigitsCalculator : IDepreciationCalculator
{
    public decimal Calculate(decimal acquisitionCost, decimal residualValue, int usefulLifeYears, int currentPeriod,
        decimal? totalUnits, decimal? unitsProduced, decimal accumulatedDepreciation)
    {
        var depreciableBase = acquisitionCost - residualValue;
        var sumOfYears = usefulLifeYears * (usefulLifeYears + 1) / 2;
        var remainingLife = usefulLifeYears - ((currentPeriod - 1) / 12);
        if (remainingLife <= 0) return 0;
        var annualDepreciation = depreciableBase * remainingLife / sumOfYears;
        return Math.Round(annualDepreciation / 12, 2);
    }
}
