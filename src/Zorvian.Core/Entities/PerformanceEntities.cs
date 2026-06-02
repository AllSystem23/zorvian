namespace Zorvian.Core.Entities;

public sealed class Objective : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<KeyResult> KeyResults { get; set; } = [];
}

public sealed class KeyResult : BaseEntity
{
    public Guid ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
}
