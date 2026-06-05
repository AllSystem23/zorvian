using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class LoanInstallment : BaseEntity
{
    public Guid EmployeeLoanId { get; set; }
    public EmployeeLoan? EmployeeLoan { get; set; }
    public int InstallmentNumber { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "pending"; // 'pending', 'paid', 'late'
}
