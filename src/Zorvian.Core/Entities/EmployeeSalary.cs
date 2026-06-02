namespace Zorvian.Core.Entities;

public sealed class EmployeeSalary : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal BaseSalary { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public Guid? DeductionTypeId { get; set; }
    public DeductionType? DeductionType { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}
