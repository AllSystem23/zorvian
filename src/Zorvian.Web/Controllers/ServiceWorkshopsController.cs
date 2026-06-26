using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/service-workshops")]
public sealed class ServiceWorkshopsController : ControllerBase
{
    private readonly WorkshopService _service;

    public ServiceWorkshopsController(WorkshopService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { error = "Service workshop not found" });
        return Ok(item);
    }

    [Audit("ServiceWorkshop", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceWorkshopRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, item);
    }

    [Audit("ServiceWorkshop", "Update")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceWorkshopRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null)
            return NotFound(new { error = "Service workshop not found" });
        return Ok(item);
    }

    [Audit("ServiceWorkshop", "Delete")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Service workshop not found" });
        return NoContent();
    }
}
