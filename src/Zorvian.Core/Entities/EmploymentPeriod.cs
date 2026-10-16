namespace Zorvian.Core.Entities;

/// <summary>
/// Período de contrato de un trabajador (historial laboral). Cada contratación
/// abre un período; cada liquidación lo cierra. Permite recontratar al MISMO
/// empleado sin perder el rastro: la antigüedad del nuevo período arranca en su
/// fecha de ingreso (la indemnización del período anterior ya se pagó).
/// </summary>
public sealed class EmploymentPeriod : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>1 = primera contratación, 2 = primera recontratación, etc.</summary>
    public int PeriodNumber { get; set; }

    /// <summary>Fecha de ingreso de ESTE período (no la del empleado original).</summary>
    public DateOnly HireDate { get; set; }

    /// <summary>Fecha de salida; null = período abierto (contrato vigente).</summary>
    public DateOnly? TerminationDate { get; set; }

    /// <summary>Motivo de salida (TerminationReason crudo, p.ej. "VoluntaryResignation").</summary>
    public string? TerminationReason { get; set; }

    /// <summary>Salario base acordado al inicio del período (snapshot).</summary>
    public decimal? BaseSalary { get; set; }

    /// <summary>Cargo del período (snapshot).</summary>
    public string? Position { get; set; }

    /// <summary>open = contrato vigente; closed = liquidado.</summary>
    public string Status { get; set; } = "open";

    /// <summary>Liquidación que cerró el período (si se aplicó vía TerminationService).</summary>
    public Guid? TerminationRecordId { get; set; }

    public string? Notes { get; set; }
}
