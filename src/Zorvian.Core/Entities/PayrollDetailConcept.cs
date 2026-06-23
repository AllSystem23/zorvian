using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class PayrollDetailConcept : BaseEntity
{
    public Guid PayrollDetailId { get; set; }
    public PayrollDetail PayrollDetail { get; set; } = null!;
    
    public string ConceptCode { get; set; } = string.Empty; // e.g., 'SALARY', 'INSS_EMP', 'IR', 'OVERTIME_25'
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsEmployerCost { get; set; } = false;
}
