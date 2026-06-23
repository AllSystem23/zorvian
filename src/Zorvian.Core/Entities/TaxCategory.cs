using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class TaxCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    
    // Cuentas contables para este tipo de impuesto
    public string SalesAccountCode { get; set; } = string.Empty;
    public string VatAccountCode { get; set; } = string.Empty;
    public Company Company { get; set; } = null!;
}
