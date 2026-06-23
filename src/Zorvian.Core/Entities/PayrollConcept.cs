namespace Zorvian.Core.Entities;

public sealed class PayrollConcept : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g., 'INSS_PAT', 'IR_SAL'
    public string Name { get; set; } = string.Empty;
    public string CalculationFormula { get; set; } = string.Empty; // e.g., "Salary * 0.0625"
    public Guid? AccountMappingId { get; set; }
    public Account? AccountMapping { get; set; }
    public bool IsActive { get; set; } = true;
}
