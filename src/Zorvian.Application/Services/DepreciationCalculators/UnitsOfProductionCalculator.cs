namespace Zorvian.Application.Services.DepreciationCalculators;

public sealed class UnitsOfProductionCalculator : IDepreciationCalculator
{
    public decimal Calculate(decimal acquisitionCost, decimal residualValue, int usefulLifeYears, int currentPeriod,
        decimal? totalUnits, decimal? unitsProduced, decimal accumulatedDepreciation)
    {
        if (totalUnits is null or 0 || unitsProduced is null or 0)
            return 0;

        var depreciableBase = acquisitionCost - residualValue;
        var unitRate = depreciableBase / totalUnits.Value;
        return Math.Round(unitRate * unitsProduced.Value, 2);
    }
}
