using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/work-orders")]
public sealed class WorkOrdersController : ControllerBase
{
    private readonly WorkOrderService _service;

    public WorkOrdersController(WorkOrderService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(new { items });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Work order not found" });
        return Ok(item);
    }

    [Audit("FleetWorkOrder", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("FleetWorkOrder", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkOrderRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Work order not found" });
        return Ok(item);
    }

    [Audit("FleetWorkOrder", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Work order not found" });
        return NoContent();
    }
}
