using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/brands")]
public sealed class VehicleBrandsController : ControllerBase
{
    private readonly VehicleBrandService _service;

    public VehicleBrandsController(VehicleBrandService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await _service.GetAllAsync();
        return Ok(brands);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var brand = await _service.GetByIdAsync(id);
        if (brand is null) return NotFound(new { error = "Brand not found" });
        return Ok(brand);
    }

    [Audit("FleetBrand", "Create")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleBrandRequest request)
    {
        var brand = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = brand.Id }, brand);
    }

    [Audit("FleetBrand", "Update")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleBrandRequest request)
    {
        var brand = await _service.UpdateAsync(id, request);
        if (brand is null) return NotFound(new { error = "Brand not found" });
        return Ok(brand);
    }

    [Audit("FleetBrand", "Delete")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Brand not found" });
        return NoContent();
    }
}
