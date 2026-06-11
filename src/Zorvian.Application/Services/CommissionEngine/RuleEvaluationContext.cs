namespace Zorvian.Application.Services.CommissionEngine;

public sealed class RuleEvaluationContext
{
    public Guid EmployeeId { get; set; }
    public Guid SaleId { get; set; }
    public string SaleType { get; set; } = string.Empty;
    public decimal SaleAmount { get; set; }
    public decimal CollectionAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMargin { get; set; }
    public string ProductLine { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
}
