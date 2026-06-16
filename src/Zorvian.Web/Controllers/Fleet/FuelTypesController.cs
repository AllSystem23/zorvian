using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/fuel-types")]
public sealed class FuelTypesController : ControllerBase
{
    private readonly FuelTypeService _service;

    public FuelTypesController(FuelTypeService service) => _service = service;

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
        if (type is null) return NotFound(new { error = "Fuel type not found" });
        return Ok(type);
    }

    [Audit("FleetFuelType", "Create")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFuelTypeRequest request)
    {
        var type = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [Audit("FleetFuelType", "Update")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFuelTypeRequest request)
    {
        var type = await _service.UpdateAsync(id, request);
        if (type is null) return NotFound(new { error = "Fuel type not found" });
        return Ok(type);
    }

    [Audit("FleetFuelType", "Delete")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Fuel type not found" });
        return NoContent();
    }
}
