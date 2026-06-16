using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/alerts")]
public sealed class FleetAlertsController : ControllerBase
{
    private readonly FleetAlertService _service;

    public FleetAlertsController(FleetAlertService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    [ProducesResponseType(typeof(FleetAlertSummary), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _service.GetAlertSummaryAsync();
        return Ok(summary);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<FleetAlertResponse>), 200)]
    public async Task<IActionResult> GetActiveAlerts()
    {
        var alerts = await _service.GetActiveAlertsAsync();
        return Ok(alerts);
    }

    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("dispatch")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> DispatchNotifications()
    {
        var count = await _service.DispatchPendingNotificationsAsync();
        return Ok(new { dispatched = count });
    }

    // ── Driver Blocking ──

    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("drivers/{driverId:guid}/block")]
    [ProducesResponseType(typeof(DriverBlockResponse), 200)]
    public async Task<IActionResult> BlockDriver(Guid driverId, [FromBody] BlockDriverRequest request)
    {
        var result = await _service.BlockDriverAsync(driverId, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("drivers/{driverId:guid}/unblock")]
    [ProducesResponseType(typeof(DriverBlockResponse), 200)]
    public async Task<IActionResult> UnblockDriver(Guid driverId)
    {
        var result = await _service.UnblockDriverAsync(driverId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("drivers/{driverId:guid}/block-status")]
    [ProducesResponseType(typeof(DriverBlockResponse), 200)]
    public async Task<IActionResult> GetDriverBlockStatus(Guid driverId)
    {
        var result = await _service.GetDriverBlockStatusAsync(driverId);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
