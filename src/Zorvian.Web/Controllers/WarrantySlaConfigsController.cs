using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("api/v1/warranty-sla-configs")]
[Authorize]
public sealed class WarrantySlaConfigsController : ControllerBase
{
    private readonly WarrantySlaConfigService _service;

    public WarrantySlaConfigsController(WarrantySlaConfigService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var config = await _service.GetByIdAsync(id);
        if (config is null) return NotFound();
        return Ok(config);
    }

    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantySlaConfigRequest request)
    {
        var config = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = config.Id }, config);
    }

    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarrantySlaConfigRequest request)
    {
        var config = await _service.UpdateAsync(id, request);
        if (config is null) return NotFound();
        return Ok(config);
    }

    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
