namespace Zorvian.Core.Entities;

public sealed class BudgetDetail : BaseEntity
{
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public decimal BudgetedAmount { get; set; }
    public string? Description { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
