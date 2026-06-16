using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/vehicle-types")]
public sealed class VehicleTypesController : ControllerBase
{
    private readonly VehicleTypeService _service;

    public VehicleTypesController(VehicleTypeService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _service.GetAllAsync();
        return Ok(types);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var type = await _service.GetByIdAsync(id);
        if (type is null) return NotFound(new { error = "Vehicle type not found" });
        return Ok(type);
    }

    [Audit("FleetVehicleType", "Create")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleTypeRequest request)
    {
        var type = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [Audit("FleetVehicleType", "Update")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleTypeRequest request)
    {
        var type = await _service.UpdateAsync(id, request);
        if (type is null) return NotFound(new { error = "Vehicle type not found" });
        return Ok(type);
    }

    [Audit("FleetVehicleType", "Delete")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Vehicle type not found" });
        return NoContent();
    }
}
