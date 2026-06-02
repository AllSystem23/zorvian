namespace Zorvian.Core.Entities;

public sealed class PayrollRun : BaseEntity
{
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalSalaries { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public string? Notes { get; set; }
    public ICollection<PayrollDetail> Details { get; set; } = [];
}
