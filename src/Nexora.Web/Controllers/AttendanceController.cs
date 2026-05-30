using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Attendance;
using Nexora.Application.Services;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Controllers;

/// <summary>
/// Controlador de asistencia. Maneja marcación de entrada/salida, registro mediante código QR y consulta de registros mensuales.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly AttendanceService _service;
    private readonly ITenantContext _tenant;

    public AttendanceController(AttendanceService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
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
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        var result = await _service.GetMyMonthlyAsync(_tenant.CurrentEmployeeId.Value, year, month);
        return Ok(result);
    }
}
