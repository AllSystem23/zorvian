using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/drivers")]
public sealed class DriversController : ControllerBase
{
    private readonly DriverService _service;

    public DriversController(DriverService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var drivers = await _service.GetAllAsync();
        return Ok(new { items = drivers });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var driver = await _service.GetByIdAsync(id);
        if (driver is null) return NotFound(new { error = "Driver not found" });
        return Ok(driver);
    }

    [Audit("FleetDriver", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDriverRequest request)
    {
        var driver = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = driver.Id }, driver);
    }

    [Audit("FleetDriver", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverRequest request)
    {
        var driver = await _service.UpdateAsync(id, request);
        if (driver is null) return NotFound(new { error = "Driver not found" });
        return Ok(driver);
    }

    [Audit("FleetDriver", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Driver not found" });
        return NoContent();
    }
}
