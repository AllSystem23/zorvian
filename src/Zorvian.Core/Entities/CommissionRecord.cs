using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class CommissionRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; } = null!;
    
    public Guid? SaleId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "pending";
    
    public Guid CompanyId { get; set; }
}
