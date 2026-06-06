using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/inventory-movements")]
public sealed class InventoryMovementsController : ControllerBase
{
    private readonly InventoryMovementService _service;

    public InventoryMovementsController(InventoryMovementService service)
    {
        _service = service;
    }

    [Audit("InventoryMovement", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.InventoryWrite)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryMovementRequest request)
    {
        try
        {
            var movement = await _service.CreateAsync(request);
            return Ok(movement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [RequirePermission(Permissions.InventoryRead)]
    public async Task<IActionResult> GetList([FromQuery] InventoryMovementFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }
}
