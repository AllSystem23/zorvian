using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class EmployeeLoan : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal InterestRate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal InstallmentAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "active"; // 'active', 'paid', 'cancelled'
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
