using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class CommissionRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid? CommissionAssignmentId { get; set; }
    public CommissionAssignment? CommissionAssignment { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;

    public Guid? PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid? SaleId { get; set; }
    public string SourceType { get; set; } = "sale";

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string Status { get; set; } = "calculated";
    public string? Description { get; set; }
    public string? CommissionRuleId { get; set; }
    public DateTime? TransactionDate { get; set; }
}
