namespace Zorvian.Core.Entities;

public sealed class EmployeeSupervisor : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid SupervisorId { get; set; }
    public Employee Supervisor { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public DateOnly? EndDate { get; set; }
}
