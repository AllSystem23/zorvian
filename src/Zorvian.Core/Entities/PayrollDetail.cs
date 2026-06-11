namespace Zorvian.Core.Entities;

public sealed class PayrollDetail : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string CollaboratorType { get; set; } = "employee";

    public decimal BaseSalary { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string? InssCode { get; set; }
    public decimal? InssDeduction { get; set; }
    public decimal? IrDeduction { get; set; }
    public decimal? OtherDeductions { get; set; }
    public decimal? CommissionsAmount { get; set; }
    public decimal? BonusesAmount { get; set; }
    public decimal? OvertimeAmount { get; set; }
    public decimal? InssEmployerDeduction { get; set; }
    public string? Details { get; set; }
    public string PaymentStatus { get; set; } = "pending";
    public string? PaymentReference { get; set; }
    public string Currency { get; set; } = "NIO";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public ICollection<PayrollDetailConcept> Concepts { get; set; } = new List<PayrollDetailConcept>();
}
