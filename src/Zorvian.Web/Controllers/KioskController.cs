using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Attendance;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de Kiosko. Permite marcación de asistencia mediante código de empleado
/// en dispositivos compartidos (tablets en sucursales).
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/kiosk")]
public sealed class KioskController : ControllerBase
{
    private readonly AttendanceService _service;

    public KioskController(AttendanceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra la entrada (check-in) de un empleado mediante su código.
    /// </summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] KioskCheckInRequest request)
    {
        try
        {
            var result = await _service.KioskCheckInAsync(request.EmployeeCode, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Registra la salida (check-out) de un empleado mediante su código.
    /// </summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] KioskCheckOutRequest request)
    {
        try
        {
            var result = await _service.KioskCheckOutAsync(request.EmployeeCode, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Busca empleados activos por código (para auto-completado en el kiosko).
    /// </summary>
    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string code, [FromQuery] int max = 10)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 2)
            return Ok(Array.Empty<EmployeeLookupItem>());

        var results = await _service.KioskLookupAsync(code, max);
        return Ok(results);
    }
}
