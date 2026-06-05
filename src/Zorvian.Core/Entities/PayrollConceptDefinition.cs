namespace Zorvian.Core.Entities;

public sealed class PayrollConceptDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty; // 'earning', 'deduction', 'employer_cost', 'provision'
    public string? CalculationMethod { get; set; } // 'fixed', 'percentage', 'formula'
    public string? DefaultFormula { get; set; }
    public bool Taxable { get; set; } = true;
    public bool InssApplicable { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
