namespace Zorvian.Core.Entities.Fleet;

public sealed class MaintenanceTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ApplicableVehicleTypes { get; set; }
    public int? DefaultIntervalKm { get; set; }
    public int? DefaultIntervalDays { get; set; }
    public int? DefaultIntervalHours { get; set; }
    public bool IsActive { get; set; } = true;
}
