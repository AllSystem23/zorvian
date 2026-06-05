namespace Zorvian.Core.Entities;

public sealed class AssetMaintenance : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;
    public DateTime MaintenanceDate { get; set; }
    public string MaintenanceType { get; set; } = "preventive";
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string? Provider { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public string Status { get; set; } = "completed";
    public Guid CompanyId { get; set; }
}
