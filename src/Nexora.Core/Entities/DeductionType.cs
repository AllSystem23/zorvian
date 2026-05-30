namespace Nexora.Core.Entities;

public sealed class DeductionType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public decimal? Rate { get; set; }
    public decimal? FixedAmount { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public int? Priority { get; set; }
}
