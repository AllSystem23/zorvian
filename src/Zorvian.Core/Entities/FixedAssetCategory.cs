namespace Zorvian.Core.Entities;

public sealed class FixedAssetCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultUsefulLifeYears { get; set; }
    public string? DefaultDepreciationMethod { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }
}
