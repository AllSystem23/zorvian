using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Dashboard;
using Nexora.Application.Services;

namespace Nexora.Web.Controllers;

/// <summary>
/// Controlador del panel principal. Proporciona indicadores clave, calendario de vacaciones y solicitudes recientes.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene los indicadores clave del dashboard (empleados, vacaciones, permisos, asistencia).
    /// </summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(DashboardKpisResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis()
    {
        var result = await _service.GetKpisAsync();
        return Ok(result);
    }

    /// <summary>
    /// Obtiene los eventos del calendario de vacaciones.
    /// </summary>
    [HttpGet("vacation-calendar")]
    [ProducesResponseType(typeof(List<VacationCalendarEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVacationCalendar()
    {
        var result = await _service.GetVacationCalendarAsync();
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las solicitudes recientes de vacaciones y permisos.
    /// </summary>
    [HttpGet("recent-requests")]
    [ProducesResponseType(typeof(List<RecentRequestItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentRequests([FromQuery] int count = 10)
    {
        var result = await _service.GetRecentRequestsAsync(count);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el dashboard ejecutivo con KPIs de todos los módulos.
    /// </summary>
    [HttpGet("executive")]
    [ProducesResponseType(typeof(ExecutiveDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutiveDashboard()
    {
        var result = await _service.GetExecutiveDashboardAsync();
        return Ok(result);
    }
}
