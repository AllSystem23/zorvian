namespace Zorvian.Application.DTOs.Employee;

/// <summary>Período de contrato del historial laboral de un trabajador.</summary>
public sealed record EmploymentPeriodResponse(
    Guid Id,
    int PeriodNumber,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    string? TerminationReason,
    string? TerminationReasonLabel,
    decimal? BaseSalary,
    string? Position,
    string Status,
    Guid? TerminationRecordId,
    string? Notes
);

/// <summary>
/// Recontratación de un trabajador dado de baja. Abre un nuevo período de
/// contrato sobre el MISMO empleado (el historial se conserva); la antigüedad
/// del nuevo período arranca en <c>HireDate</c> porque la indemnización del
/// período anterior ya fue liquidada y pagada.
/// </summary>
public sealed record RehireEmployeeRequest(
    DateOnly HireDate,
    decimal? Salary = null,
    string? Position = null,
    Guid? DepartmentId = null,
    Guid? BranchId = null,
    string? Notes = null
);
