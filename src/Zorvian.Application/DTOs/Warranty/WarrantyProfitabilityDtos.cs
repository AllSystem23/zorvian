namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyCostSummary(
    decimal TotalCost,
    decimal PartsCost,
    decimal LaborCost,
    decimal OtherCost,
    int BilledCostCount
);

public sealed record WarrantyProfitabilityReport(
    decimal TotalWarrantyCost,
    decimal TotalSaleValue,
    decimal ProfitMarginPercent,
    List<BrandProfitability> ByBrand,
    List<CostCategoryBreakdown> CostBreakdown,
    List<MonthlyCostTrend> MonthlyTrend,
    DateTime From,
    DateTime To
);

public sealed record BrandProfitability(
    string BrandName,
    int WarrantyCount,
    decimal TotalWarrantyCost,
    decimal TotalSaleValue,
    decimal ProfitMarginPercent
);

public sealed record CostCategoryBreakdown(
    string Category,
    decimal Total,
    decimal Percent
);

public sealed record MonthlyCostTrend(
    int Year,
    int Month,
    decimal Total
);
