using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Inventory;
using Nexora.Application.Services;
using Nexora.Web.Filters;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inventory-movements")]
public sealed class InventoryMovementsController : ControllerBase
{
    private readonly InventoryMovementService _service;

    public InventoryMovementsController(InventoryMovementService service)
    {
        _service = service;
    }

    [Audit("InventoryMovement", "Create")]
    [HttpPost]
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
    public async Task<IActionResult> GetList([FromQuery] InventoryMovementFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }
}
