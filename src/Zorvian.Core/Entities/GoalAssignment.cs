using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class GoalAssignment : BaseEntity
{
    public Guid GoalDefinitionId { get; set; }
    public GoalDefinition GoalDefinition { get; set; } = null!;

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? TeamId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? StretchValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BaseLine { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Weight { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? MinimumGate { get; set; }

    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<GoalProgress> ProgressEntries { get; set; } = [];
    public ICollection<IncentivePayment> IncentivePayments { get; set; } = [];
}
