namespace Zorvian.Core.Entities;

public sealed class ProviderBrand
{
    public Guid ProviderId { get; set; }
    public WarrantyProvider Provider { get; set; } = null!;
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public int SlaHours { get; set; } = 168;
}
