namespace Zorvian.Core.Entities;

public sealed class CommissionScheme : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CommissionType { get; set; } = "sale";
    public string CalculationMethod { get; set; } = "percentage";
    public string Status { get; set; } = "active";
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public bool IsTeamBased { get; set; }
    public bool RequiresMinimumGoal { get; set; }
    public decimal? MinimumGoalValue { get; set; }
    public bool ApplyClawback { get; set; } = true;

    public ICollection<CommissionRule> Rules { get; set; } = [];
    public ICollection<CommissionAssignment> Assignments { get; set; } = [];
}
