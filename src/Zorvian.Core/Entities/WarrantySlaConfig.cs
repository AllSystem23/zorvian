using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class WarrantySlaConfig : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CoverageType { get; set; }
    public string? Priority { get; set; }
    public int TotalHours { get; set; }
    public int? WorkshopHours { get; set; }
    public int? ProviderHours { get; set; }
    public int? DeliveryHours { get; set; }
    public int AlertThresholdPct { get; set; } = 80;
    public bool IsActive { get; set; } = true;
}
