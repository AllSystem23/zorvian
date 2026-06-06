namespace Zorvian.Core.Entities;

public sealed class WorkshopBrand
{
    public Guid WorkshopId { get; set; }
    public ServiceWorkshop Workshop { get; set; } = null!;
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public int SlaHours { get; set; } = 72;
}
