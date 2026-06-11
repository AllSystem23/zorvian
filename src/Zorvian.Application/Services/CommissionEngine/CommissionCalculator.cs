namespace Zorvian.Application.Services.CommissionEngine;

public sealed class CommissionCalculator
{
    public decimal CalculatePercentage(decimal baseAmount, decimal rate)
    {
        return Math.Round(baseAmount * (rate / 100m), 2);
    }

    public decimal CalculateFixed(decimal fixedAmount)
    {
        return Math.Round(fixedAmount, 2);
    }

    public decimal CalculateTiered(decimal baseAmount, List<TieredRate> tiers)
    {
        var sorted = tiers.OrderBy(t => t.From).ToList();
        var total = 0m;
        var remaining = baseAmount;

        foreach (var tier in sorted)
        {
            if (remaining <= 0) break;

            var tierAmount = tier.To.HasValue
                ? Math.Min(remaining, tier.To.Value - tier.From)
                : remaining;

            total += tierAmount * (tier.Rate / 100m);
            remaining -= tierAmount;
        }

        return Math.Round(total, 2);
    }

    public decimal CalculateMixed(decimal baseSalary, decimal commissionAmount)
    {
        return Math.Round(baseSalary + commissionAmount, 2);
    }
}

public sealed record TieredRate(decimal From, decimal? To, decimal Rate);
