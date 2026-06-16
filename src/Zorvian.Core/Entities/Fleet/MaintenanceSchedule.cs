namespace Zorvian.Core.Entities.Fleet;

public sealed class MaintenanceSchedule : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid? TemplateId { get; set; }
    public MaintenanceTemplate? Template { get; set; }
    public string ScheduleType { get; set; } = "Date";
    public int IntervalValue { get; set; }
    public DateTime? NextExecutionDate { get; set; }
    public decimal? NextExecutionKm { get; set; }
    public decimal? NextExecutionHourMeter { get; set; }
    public DateTime? LastExecutionDate { get; set; }
    public decimal? LastExecutionKm { get; set; }
    public int ToleranceValue { get; set; }
    public string Status { get; set; } = "Active";
}
