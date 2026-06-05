namespace Zorvian.Core.Entities;

public sealed class AssetRevaluation : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;
    public DateTime RevaluationDate { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal NewValue { get; set; }
    public decimal PreviousAccumulatedDepreciation { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
    public Guid? AccountingEntryId { get; set; }
    public AccountingEntry? AccountingEntry { get; set; }
    public Guid CompanyId { get; set; }
}
