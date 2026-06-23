namespace Zorvian.Core.Entities;

public sealed class EmployeePayrollExemption : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    
    public Guid PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}
