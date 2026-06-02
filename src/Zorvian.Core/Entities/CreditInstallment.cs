namespace Zorvian.Core.Entities;

public sealed class CreditInstallment : BaseEntity
{
    public Guid CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "pending";
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
