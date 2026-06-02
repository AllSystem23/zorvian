namespace Nexora.Core.Entities;

public sealed class CreditPayment : BaseEntity
{
    public Guid CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    public Guid? CreditInstallmentId { get; set; }
    public CreditInstallment? CreditInstallment { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? CashRegisterId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
