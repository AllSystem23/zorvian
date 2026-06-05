namespace Zorvian.Core.Entities;

public sealed class BenefitProvision : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string BenefitType { get; set; } = string.Empty; // 'aguinaldo', 'vacation', 'indemnity'
    public decimal Amount { get; set; }
    public DateOnly CalculationDate { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }
}
