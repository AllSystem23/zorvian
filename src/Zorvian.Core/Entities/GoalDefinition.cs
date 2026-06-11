namespace Zorvian.Core.Entities;

public sealed class GoalDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GoalType { get; set; } = string.Empty;
    public string MetricType { get; set; } = "amount";
    public string Frequency { get; set; } = "monthly";
    public int EvaluationPeriodDays { get; set; } = 30;
    public string DataSource { get; set; } = string.Empty;
    public string? CalculationFormula { get; set; }
    public bool HasGateCondition { get; set; }
    public string? GateDescription { get; set; }
    public string? GateFormula { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<GoalAssignment> Assignments { get; set; } = [];
}
