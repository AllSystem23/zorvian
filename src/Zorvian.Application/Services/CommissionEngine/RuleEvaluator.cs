using System.Text.Json;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.CommissionEngine;

public sealed class RuleEvaluator
{
    public bool Evaluate(CommissionRule rule, RuleEvaluationContext context)
    {
        var value = rule.ConditionType.ToLowerInvariant() switch
        {
            "sale_amount" => (object)context.SaleAmount,
            "collection_amount" => (object)context.CollectionAmount,
            "profit_margin" => (object)context.ProfitMargin,
            "product_line" => context.ProductLine,
            "product_category" => context.ProductCategory,
            "brand" => context.Brand,
            "branch" => context.BranchId,
            "sale_type" => context.SaleType,
            _ => null
        };

        if (value is null) return false;

        return rule.ConditionOperator.ToLowerInvariant() switch
        {
            "equals" => value.ToString() == rule.ConditionValue,
            "greater_than" when value is decimal d => d > ParseDecimal(rule.ConditionValue),
            "less_than" when value is decimal d => d < ParseDecimal(rule.ConditionValue),
            "between" when value is decimal d => EvaluateBetween(d, rule.ConditionValue),
            "in" => rule.ConditionValue.Split(',').Select(v => v.Trim()).Contains(value.ToString()),
            "contains" => value.ToString()?.Contains(rule.ConditionValue) == true,
            _ => false
        };
    }

    public decimal CalculateAmount(decimal baseAmount, CommissionRule rule, CommissionCalculator calculator)
    {
        return rule.CalculationType.ToLowerInvariant() switch
        {
            "percentage" => calculator.CalculatePercentage(baseAmount, rule.Rate ?? 0),
            "fixed" => calculator.CalculateFixed(rule.Rate ?? 0),
            "tiered" => CalculateTieredFromJson(baseAmount, rule.CalculationValue, calculator),
            _ => 0
        };
    }

    public decimal DetermineBaseAmount(RuleEvaluationContext context, CommissionRule rule)
    {
        return rule.ApplyOn.ToLowerInvariant() switch
        {
            "gross" => context.SaleAmount,
            "net" => context.SaleAmount - 0,
            "profit" => context.ProfitAmount,
            "collection" => context.CollectionAmount,
            _ => context.SaleAmount
        };
    }

    private static bool EvaluateBetween(decimal value, string conditionValue)
    {
        try
        {
            var parts = conditionValue.Split('-');
            if (parts.Length != 2) return false;
            var min = decimal.Parse(parts[0].Trim());
            var max = decimal.Parse(parts[1].Trim());
            return value >= min && value <= max;
        }
        catch
        {
            return false;
        }
    }

    private static decimal CalculateTieredFromJson(decimal baseAmount, string json, CommissionCalculator calculator)
    {
        try
        {
            var tiers = JsonSerializer.Deserialize<List<TieredRate>>(json);
            if (tiers is null || tiers.Count == 0) return 0;
            return calculator.CalculateTiered(baseAmount, tiers);
        }
        catch
        {
            return 0;
        }
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value.Trim(), out var result) ? result : 0;
    }
}
