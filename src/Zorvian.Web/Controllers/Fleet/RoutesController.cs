using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/routes")]
public sealed class RoutesController : ControllerBase
{
    private readonly RouteService _service;

    public RoutesController(RouteService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var routes = await _service.GetAllAsync();
        return Ok(new { items = routes });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var route = await _service.GetByIdAsync(id);
        if (route is null) return NotFound(new { error = "Route not found" });
        return Ok(route);
    }

    [Audit("FleetRoute", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRouteRequest request)
    {
        var route = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = route.Id }, route);
    }

    [Audit("FleetRoute", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRouteRequest request)
    {
        var route = await _service.UpdateAsync(id, request);
        if (route is null) return NotFound(new { error = "Route not found" });
        return Ok(route);
    }

    [Audit("FleetRoute", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Route not found" });
        return NoContent();
    }
}
