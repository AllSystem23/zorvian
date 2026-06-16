using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/geofences")]
public sealed class GeofencesController : ControllerBase
{
    private readonly GeofenceService _service;

    public GeofencesController(GeofenceService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    [ProducesResponseType(typeof(List<GeofenceResponse>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(new { items });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GeofenceResponse), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound(new { error = "Geofence not found" });
        return Ok(item);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<GeofenceResponse>), 200)]
    public async Task<IActionResult> GetActive()
    {
        var items = await _service.GetActiveAsync();
        return Ok(new { items });
    }

    [Audit("Geofence", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    [ProducesResponseType(typeof(GeofenceResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateGeofenceRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("Geofence", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GeofenceResponse), 200)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGeofenceRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item == null) return NotFound(new { error = "Geofence not found" });
        return Ok(item);
    }

    [Audit("Geofence", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Geofence not found" });
        return NoContent();
    }
}
