using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/license-categories")]
public sealed class DriverLicenseCategoriesController : ControllerBase
{
    private readonly DriverLicenseCategoryService _service;

    public DriverLicenseCategoriesController(DriverLicenseCategoryService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? countryCode)
    {
        var categories = await _service.GetAllAsync(countryCode);
        return Ok(categories);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _service.GetByIdAsync(id);
        if (category is null) return NotFound(new { error = "License category not found" });
        return Ok(category);
    }

    [Audit("FleetLicenseCategory", "Create")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDriverLicenseCategoryRequest request)
    {
        var category = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [Audit("FleetLicenseCategory", "Update")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverLicenseCategoryRequest request)
    {
        var category = await _service.UpdateAsync(id, request);
        if (category is null) return NotFound(new { error = "License category not found" });
        return Ok(category);
    }

    [Audit("FleetLicenseCategory", "Delete")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "License category not found" });
        return NoContent();
    }
}
