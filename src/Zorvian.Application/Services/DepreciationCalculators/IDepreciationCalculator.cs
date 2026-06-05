namespace Zorvian.Application.Services.DepreciationCalculators;

public interface IDepreciationCalculator
{
    decimal Calculate(decimal acquisitionCost, decimal residualValue, int usefulLifeYears, int currentPeriod,
        decimal? totalUnits, decimal? unitsProduced, decimal accumulatedDepreciation);
}
