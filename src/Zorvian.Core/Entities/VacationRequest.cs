namespace Zorvian.Core.Entities;

public sealed class VacationRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public decimal BusinessDays { get; set; }
    public string? Comments { get; set; }
    public string Status { get; set; } = "pending";
    public string? RejectionReason { get; set; }
    public bool IsAdvanced { get; set; }
    /// <summary>
    /// Corrida de nómina que pagó estas vacaciones (vacaciones incluidas en una planilla).
    /// Marca el consumo de saldo hecho al aprobar la corrida para poder revertirlo si la
    /// corrida se cancela, e identifica solicitudes pagadas por planilla (no por recibo RV).
    /// </summary>
    public Guid? PaidByPayrollRunId { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public ICollection<ApprovalFlow> ApprovalSteps { get; set; } = [];
}
