namespace Zorvian.Core.Entities;

public sealed class ExchangeRate : BaseEntity
{
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
}
