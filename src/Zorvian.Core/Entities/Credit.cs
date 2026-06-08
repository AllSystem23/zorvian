namespace Zorvian.Core.Entities;

public sealed class Credit : BaseEntity
{
    public string CreditNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal FinancedAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public decimal InterestAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<CreditInstallment> Installments { get; set; } = [];
    public ICollection<CreditPayment> Payments { get; set; } = [];
    public ICollection<CreditRefinancing> Refinancings { get; set; } = [];
}
