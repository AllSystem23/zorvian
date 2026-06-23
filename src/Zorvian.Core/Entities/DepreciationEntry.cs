namespace Zorvian.Core.Entities;

public sealed class DepreciationEntry : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue { get; set; }
    public Guid? AccountingEntryId { get; set; }
    public AccountingEntry? AccountingEntry { get; set; }
    public string? Notes { get; set; }
}
