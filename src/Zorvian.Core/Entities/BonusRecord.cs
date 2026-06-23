using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class BonusRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    
    public string BonusType { get; set; } = string.Empty; // 'productivity', 'performance', etc.
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
}
