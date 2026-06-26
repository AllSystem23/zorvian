namespace Zorvian.Application.DTOs.Goal;

public sealed class GoalDashboardDto
{
    public decimal GlobalCompliance { get; set; }
    public decimal IncentiveBudget { get; set; }
    public int TotalGoals { get; set; }
    public int ActiveGoals { get; set; }
    public int TotalAssignments { get; set; }
    public List<GoalStatsDto> GoalStats { get; set; } = [];
    public List<LowPerformerDto> LowPerformers { get; set; } = [];
}

public sealed class GoalStatsDto
{
    public Guid GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public string GoalType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Participants { get; set; }
    public decimal AverageCompliance { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
}

public sealed class LowPerformerDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public decimal CompliancePercentage { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
}
