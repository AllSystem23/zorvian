namespace Zorvian.Application.DTOs.ML;

public sealed class PurchaseRecommendationDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public decimal CostPrice { get; set; }
    public double AverageDailyDemand { get; set; }
    public double DaysUntilStockout { get; set; }
    public int RecommendedQuantity { get; set; }
    public int LastMonthSold { get; set; }
    public string? SupplierName { get; set; }
    public Guid? SupplierId { get; set; }
    public string Priority { get; set; } = "low";
}

public sealed class PurchaseRecommendationSummaryDto
{
    public int TotalProducts { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int HealthyCount { get; set; }
    public decimal TotalRecommendedCost { get; set; }
    public List<PurchaseRecommendationDto> Recommendations { get; set; } = [];
}
