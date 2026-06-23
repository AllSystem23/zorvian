namespace Zorvian.Core.Entities;

public sealed class Budget : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public decimal BudgetedAmount { get; set; }
}
