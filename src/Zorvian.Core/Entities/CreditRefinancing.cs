namespace Zorvian.Core.Entities;

public sealed class CreditRefinancing : BaseEntity
{
    public Guid CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    public decimal PreviousBalance { get; set; }
    public decimal PreviousInterestRate { get; set; }
    public int PreviousInstallmentCount { get; set; }
    public decimal PreviousInstallmentAmount { get; set; }
    public decimal NewFinancedAmount { get; set; }
    public decimal NewInterestRate { get; set; }
    public int NewInstallmentCount { get; set; }
    public decimal NewInstallmentAmount { get; set; }
    public decimal NewTotalAmount { get; set; }
    public decimal NewInterestAmount { get; set; }
    public DateOnly NewStartDate { get; set; }
    public DateOnly NewEndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
