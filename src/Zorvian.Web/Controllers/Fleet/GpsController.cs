using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/gps")]
public sealed class GpsController : ControllerBase
{
    private readonly GpsService _service;

    public GpsController(GpsService service) => _service = service;

    /// <summary>Receive a single GPS position from a device.</summary>
    [HttpPost("positions")]
    [ProducesResponseType(typeof(GpsPositionResponse), 200)]
    public async Task<IActionResult> ReceivePosition([FromBody] ReceiveGpsPositionRequest request)
    {
        var result = await _service.ReceivePositionAsync(request);
        if (result == null) return NotFound(new { error = "Vehicle not found for device" });
        return Ok(result);
    }

    /// <summary>Receive bulk GPS positions (batch from Traccar/device).</summary>
    [HttpPost("positions/bulk")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> ReceiveBulk([FromBody] BulkReceiveGpsRequest request)
    {
        var count = await _service.ReceiveBulkAsync(request);
        return Ok(new { received = count });
    }

    /// <summary>Get latest position for all vehicles (real-time map).</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("fleet")]
    [ProducesResponseType(typeof(List<VehiclePositionSummary>), 200)]
    public async Task<IActionResult> GetFleetPositions()
    {
        var positions = await _service.GetFleetPositionsAsync();
        return Ok(positions);
    }

    /// <summary>Get latest position for a specific vehicle.</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("vehicles/{vehicleId:guid}/latest")]
    [ProducesResponseType(typeof(GpsPositionResponse), 200)]
    public async Task<IActionResult> GetLatestPosition(Guid vehicleId)
    {
        var result = await _service.GetLatestPositionAsync(vehicleId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Get GPS history for a vehicle within a date range.</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("vehicles/{vehicleId:guid}/history")]
    [ProducesResponseType(typeof(GpsHistoryResponse), 200)]
    public async Task<IActionResult> GetVehicleHistory(
        Guid vehicleId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await _service.GetVehicleHistoryAsync(vehicleId, from, to);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Check if a point is inside any active geofence.</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("geofence/check")]
    [ProducesResponseType(typeof(GeofenceCheckResponse), 200)]
    public async Task<IActionResult> CheckGeofence([FromBody] GeofenceCheckRequest request)
    {
        var result = await _service.CheckPointInGeofenceAsync(request.Latitude, request.Longitude);
        return Ok(result);
    }

    /// <summary>Clean up old GPS positions.</summary>
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("cleanup")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Cleanup([FromQuery] int olderThanDays = 90)
    {
        var deleted = await _service.CleanupOldPositionsAsync(olderThanDays);
        return Ok(new { deleted });
    }
}
