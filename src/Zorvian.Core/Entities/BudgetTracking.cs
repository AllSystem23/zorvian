namespace Zorvian.Core.Entities;

public sealed class BudgetTracking : BaseEntity
{
    public Guid BudgetDetailId { get; set; }
    public BudgetDetail BudgetDetail { get; set; } = null!;
    public Guid AccountId { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal BudgetedAmount { get; set; }
    public decimal Variance => BudgetedAmount - ActualAmount;
    public decimal VariancePercentage => BudgetedAmount > 0 ? Math.Round(Variance / BudgetedAmount * 100, 2) : 0;
    public int Month { get; set; }
    public int Year { get; set; }
    public DateOnly? TrackedAt { get; set; }
    public string? SourceReference { get; set; }
    public string? Notes { get; set; }
}
