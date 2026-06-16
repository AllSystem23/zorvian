using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/routes")]
public sealed class RouteOptimizationController : ControllerBase
{
    private readonly RouteOptimizationService _service;

    public RouteOptimizationController(RouteOptimizationService service) => _service = service;

    /// <summary>Optimize a route using nearest-neighbor TSP algorithm.</summary>
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("{routeId:guid}/optimize")]
    [ProducesResponseType(typeof(OptimizedRouteResponse), 200)]
    public async Task<IActionResult> OptimizeRoute(Guid routeId, [FromQuery] string criteria = "Distance")
    {
        var result = await _service.OptimizeRouteAsync(new OptimizeRouteRequest(routeId, criteria));
        if (result == null) return NotFound(new { error = "Route not found" });
        return Ok(result);
    }

    /// <summary>Automatically assign the best available driver to a route.</summary>
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("{routeId:guid}/assign-driver")]
    [ProducesResponseType(typeof(DriverAssignmentResponse), 200)]
    public async Task<IActionResult> AssignDriver(Guid routeId, [FromBody] AssignDriverRequest? request = null)
    {
        try
        {
            var req = request != null
                ? new AssignDriverRequest(routeId, request.PreferredDriverId)
                : new AssignDriverRequest(routeId);
            var result = await _service.AssignDriverAsync(req);
            if (result == null) return NotFound(new { error = "Route not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get all available drivers for assignment.</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("available-drivers")]
    [ProducesResponseType(typeof(List<AvailableDriverResponse>), 200)]
    public async Task<IActionResult> GetAvailableDrivers()
    {
        var drivers = await _service.GetAvailableDriversAsync();
        return Ok(drivers);
    }

    /// <summary>Calculate ETAs for all deliveries in a route.</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{routeId:guid}/etas")]
    [ProducesResponseType(typeof(DeliveryEtaResponse), 200)]
    public async Task<IActionResult> CalculateEtas(Guid routeId)
    {
        var result = await _service.CalculateDeliveryEtasAsync(new DeliveryEtaRequest(routeId));
        if (result == null) return NotFound(new { error = "Route not found" });
        return Ok(result);
    }
}
