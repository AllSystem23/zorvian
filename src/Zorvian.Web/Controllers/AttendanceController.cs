using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Attendance;
using Zorvian.Application.DTOs.ML;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de asistencia. Maneja marcación de entrada/salida, registro mediante código QR y consulta de registros mensuales.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly AttendanceService _service;
    private readonly ITenantContext _tenant;
    private readonly AbsenteeismPredictionService _mlService;
    private readonly ZorvianDbContext _db;

    public AttendanceController(AttendanceService service, ITenantContext tenant, AbsenteeismPredictionService mlService, ZorvianDbContext db)
    {
        _service = service;
        _tenant = tenant;
        _mlService = mlService;
        _db = db;
    }

    /// <summary>
    /// Obtiene las predicciones de riesgo de ausentismo para los empleados (Solo Admin).
    /// </summary>
    [HttpGet("predictions")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> GetAbsenteeismPredictions()
    {
        var employees = await _db.Employees.Where(e => e.TenantId == _tenant.TenantId).ToListAsync();
        var predictions = new List<object>();

        foreach (var emp in employees)
        {
            var data = new AttendanceData
            {
                DayOfWeek = (float)DateTime.UtcNow.DayOfWeek,
                Month = (float)DateTime.UtcNow.Month,
                IsHoliday = 0,
                PreviousAbsenceCount = await _db.AttendanceRecords.CountAsync(ar => ar.EmployeeId == emp.Id && ar.Status != "present")
            };

            var prediction = _mlService.Predict(data);
            predictions.Add(new
            {
                EmployeeId = emp.Id,
                FullName = $"{emp.FirstName} {emp.LastName}",
                Risk = prediction.Prediction ? "Alto" : "Bajo",
                Probability = prediction.Probability
            });
        }

        return Ok(predictions);
    }

    /// <summary>
    /// Registra la entrada (check-in) del empleado autenticado.
    /// </summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        try
        {
            var result = await _service.CheckInAsync(_tenant.CurrentEmployeeId.Value, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Registra la salida (check-out) del empleado autenticado.
    /// </summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        try
        {
            var result = await _service.CheckOutAsync(_tenant.CurrentEmployeeId.Value, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Registra la entrada del empleado mediante un código QR.
    /// </summary>
    [HttpPost("qr-check-in")]
    public async Task<IActionResult> QRCheckIn([FromBody] QRCheckInRequest request)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        try
        {
            var result = await _service.QRCheckInAsync(_tenant.CurrentEmployeeId.Value, _tenant.TenantId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene los registros de asistencia mensual del empleado autenticado.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyMonthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        if (_tenant.CurrentEmployeeId is null && !isSuperAdmin)
            return Unauthorized(new { error = "No employee profile linked" });

        if (isSuperAdmin && _tenant.CurrentEmployeeId is null)
            return Ok(new AttendanceSummaryResponse(0, 0, 0, 0, 0, []));

        var result = await _service.GetMyMonthlyAsync(_tenant.CurrentEmployeeId!.Value, year, month);
        return Ok(result);
    }
}
