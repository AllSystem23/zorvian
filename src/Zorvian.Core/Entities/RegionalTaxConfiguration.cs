namespace Zorvian.Core.Entities;

public sealed class RegionalTaxConfiguration : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty; // e.g., 'NI', 'CR', 'HN'
    public string TaxType { get; set; } = string.Empty; // e.g., 'IVA', 'IR', 'ISV'
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
