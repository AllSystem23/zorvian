namespace Zorvian.Core.Entities;

public sealed class PayrollRun : BaseEntity
{
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalSalaries { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public decimal TotalEmployerCosts { get; set; }
    public int EmployeeCount { get; set; }
    public string Currency { get; set; } = "NIO";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public string? Notes { get; set; }
    public ICollection<PayrollDetail> Details { get; set; } = [];
    public ICollection<ApprovalFlow> ApprovalSteps { get; set; } = [];
}
