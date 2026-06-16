namespace Zorvian.Core.Entities.Fleet;

public sealed class WorkOrder : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public DateTime ReportDateTime { get; set; }
    public Guid? FailureTypeId { get; set; }
    public FailureType? FailureType { get; set; }
    public string? ProblemDescription { get; set; }
    public string? Diagnosis { get; set; }
    public string? RootCause { get; set; }
    public string? SolutionApplied { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Reported";
    public Guid? WorkshopId { get; set; }
    public Workshop? Workshop { get; set; }
    public string? MechanicResponsible { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DowntimeHours { get; set; }
    public decimal CostEst { get; set; }
    public decimal CostLabor { get; set; }
    public decimal CostParts { get; set; }
    public decimal CostTotal { get; set; }
    public string? DocumentsJson { get; set; }
    public Guid? ApprovedBy { get; set; }
}
