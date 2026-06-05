using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class OvertimeRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    
    public DateTime Date { get; set; }
    public string OvertimeType { get; set; } = string.Empty; // 'diurnal', 'nocturnal', 'holiday'
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    
    public Guid CompanyId { get; set; }
}
