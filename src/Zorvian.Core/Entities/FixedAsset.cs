namespace Zorvian.Core.Entities;

public sealed class FixedAsset : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public FixedAssetCategory? Category { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public DateTime AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? InvoiceReference { get; set; }
    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public int UsefulLifeYears { get; set; }
    public decimal ResidualValue { get; set; }
    public string DepreciationMethod { get; set; } = "StraightLine";
    public decimal? TotalUnits { get; set; }
    public decimal? UnitsProduced { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? AssignedTo { get; set; }
    public string Status { get; set; } = "active";
    public bool IsActive { get; set; } = true;
    public Guid BranchId { get; set; }
    public string? ImageUrl { get; set; }

    public ICollection<DepreciationEntry> DepreciationEntries { get; set; } = [];
    public ICollection<AssetRevaluation> Revaluations { get; set; } = [];
    public ICollection<AssetMaintenance> MaintenanceRecords { get; set; } = [];
    public AssetDisposal? Disposal { get; set; }
}
