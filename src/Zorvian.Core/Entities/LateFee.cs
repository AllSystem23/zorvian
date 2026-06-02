namespace Zorvian.Core.Entities;

public sealed class LateFee : BaseEntity
{
    public Guid CreditInstallmentId { get; set; }
    public CreditInstallment CreditInstallment { get; set; } = null!;
    public Guid CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    public int DaysOverdue { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = "pending";
    public DateOnly CalculatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
