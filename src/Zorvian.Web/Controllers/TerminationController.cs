using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Ciclo de vida del contrato: cálculo de liquidación, aplicación de la
/// liquidación (termina al empleado y cierra su período de contrato) y
/// recontratación (abre un nuevo período sobre el mismo empleado — historial
/// laboral conservado).
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class TerminationController : ControllerBase
{
    private readonly TerminationService _service;

    public TerminationController(TerminationService service) => _service = service;

    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        Guid employeeId,
        TerminationReason reason,
        DateOnly terminationDate,
        DateOnly? paidThroughDate = null,
        decimal overtimeHours = 0,
        decimal overtimePay = 0,
        decimal? inssEmployeeRate = null,
        decimal? inssEmployerRate = null,
        decimal? otherEmployerRate = null,
        string? otherEmployerName = null) =>
        Ok(await _service.CalculateAsync(
            employeeId,
            reason,
            terminationDate,
            paidThroughDate,
            overtimeHours,
            overtimePay,
            inssEmployeeRate,
            inssEmployerRate,
            otherEmployerRate,
            otherEmployerName));

    /// <summary>
    /// Aplica una liquidación en draft: marca al empleado como terminado,
    /// cierra su período de contrato, desactiva el salario activo y liquida
    /// los saldos de vacaciones del año.
    /// </summary>
    [Audit("Termination", "Apply")]
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id) =>
        Ok(await _service.ApplyTerminationAsync(id));

    /// <summary>
    /// Recontrata a un trabajador dado de baja sobre el MISMO registro
    /// (conserva código, documentos e historial). Abre un nuevo período de
    /// contrato con la nueva fecha de ingreso — la antigüedad reinicia porque
    /// el período anterior ya fue liquidado y pagado.
    /// </summary>
    [Audit("Employee", "Rehire")]
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost("rehire/{employeeId:guid}")]
    public async Task<IActionResult> Rehire(Guid employeeId, [FromBody] RehireEmployeeRequest request)
    {
        var employee = await _service.RehireAsync(employeeId, request);
        return Ok(new
        {
            employee.Id,
            employee.Status,
            employee.HireDate,
            employee.Salary,
            employee.Position,
        });
    }

    /// <summary>Historial de contratos (períodos) de un trabajador.</summary>
    [Audit("Employee", "ReadEmploymentPeriods")]
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("periods/{employeeId:guid}")]
    public async Task<IActionResult> GetPeriods(Guid employeeId)
    {
        var periods = await _service.GetEmploymentPeriodsAsync(employeeId);
        return Ok(periods.Select(p => new EmploymentPeriodResponse(
            p.Id,
            p.PeriodNumber,
            p.HireDate,
            p.TerminationDate,
            p.TerminationReason,
            TerminationReasonLabel(p.TerminationReason),
            p.BaseSalary,
            p.Position,
            p.Status,
            p.TerminationRecordId,
            p.Notes
        )));
    }

    private static string? TerminationReasonLabel(string? reason) => reason switch
    {
        "VoluntaryResignation" => "Renuncia con 15 días",
        "ImmediateResignation" => "Renuncia inmediata",
        "JustifiedDismissal" => "Despido justificado",
        "UnjustifiedDismissal" => "Causa justa (Art. 48 CT)",
        "MutualAgreement" => "Mutuo acuerdo",
        _ => reason,
    };
}
