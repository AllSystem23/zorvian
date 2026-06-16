using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly TripService _service;

    public TripsController(TripService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trips = await _service.GetAllAsync();
        return Ok(new { items = trips });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var trip = await _service.GetByIdAsync(id);
        if (trip is null) return NotFound(new { error = "Trip not found" });
        return Ok(trip);
    }

    [Audit("FleetTrip", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTripRequest request)
    {
        var trip = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    [Audit("FleetTrip", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTripRequest request)
    {
        var trip = await _service.UpdateAsync(id, request);
        if (trip is null) return NotFound(new { error = "Trip not found" });
        return Ok(trip);
    }

    [Audit("FleetTrip", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Trip not found" });
        return NoContent();
    }
}
