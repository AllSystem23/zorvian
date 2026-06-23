using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class SalaryAdvance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedAmount { get; set; }
    public int DeductionInstallments { get; set; } = 1;
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DeductionPerPeriod { get; set; }
    public string Status { get; set; } = "pending"; // 'pending', 'approved', 'rejected', 'deducted'
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedBy { get; set; }
    public Guid BranchId { get; set; }
}
