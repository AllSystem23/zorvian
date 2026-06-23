namespace Zorvian.Core.Entities;

public sealed class AssetDisposal : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;
    public DateTime DisposalDate { get; set; }
    public string DisposalType { get; set; } = "sale";
    public decimal? SaleAmount { get; set; }
    public decimal NetBookValueAtDisposal { get; set; }
    public decimal GainOrLoss { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
    public Guid? AccountingEntryId { get; set; }
    public AccountingEntry? AccountingEntry { get; set; }
}
