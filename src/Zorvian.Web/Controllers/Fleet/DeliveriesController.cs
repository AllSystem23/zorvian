using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/deliveries")]
public sealed class DeliveriesController : ControllerBase
{
    private readonly DeliveryService _service;

    public DeliveriesController(DeliveryService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var deliveries = await _service.GetAllAsync();
        return Ok(new { items = deliveries });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var delivery = await _service.GetByIdAsync(id);
        if (delivery is null) return NotFound(new { error = "Delivery not found" });
        return Ok(delivery);
    }

    [Audit("FleetDelivery", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryRequest request)
    {
        var delivery = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);
    }

    [Audit("FleetDelivery", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeliveryRequest request)
    {
        var delivery = await _service.UpdateAsync(id, request);
        if (delivery is null) return NotFound(new { error = "Delivery not found" });
        return Ok(delivery);
    }

    [Audit("FleetDelivery", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Delivery not found" });
        return NoContent();
    }
}
