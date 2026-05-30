namespace Nexora.Core.Entities;

public sealed class PayrollDetail : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string? InssCode { get; set; }
    public decimal? InssDeduction { get; set; }
    public decimal? IrDeduction { get; set; }
    public decimal? OtherDeductions { get; set; }
    public string? Details { get; set; }
}
