using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class GoalProgress : BaseEntity
{
    public Guid GoalAssignmentId { get; set; }
    public GoalAssignment GoalAssignment { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompliancePercentage { get; set; }

    public DateOnly EvaluationDate { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? SourceData { get; set; }
}
