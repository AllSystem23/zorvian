using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class IncentivePayment : BaseEntity
{
    public Guid IncentiveId { get; set; }
    public Incentive Incentive { get; set; } = null!;

    public Guid GoalAssignmentId { get; set; }
    public GoalAssignment GoalAssignment { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompliancePercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CalculatedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Adjustments { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FinalAmount { get; set; }

    public string Status { get; set; } = "pending";

    public Guid? PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public DateTime? PaidAt { get; set; }
}
