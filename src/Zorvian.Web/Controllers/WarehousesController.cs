using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warehouse;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Bodegas: ubicaciones físicas de stock dentro de una sucursal
/// (Compañía → Sucursales → Bodegas). CRUD + promoción a default.
/// </summary>
[Authorize]
[ApiController]
[Route("zorvian/v1/warehouses")]
public sealed class WarehousesController : ControllerBase
{
    private readonly WarehouseService _service;

    public WarehousesController(WarehouseService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] Guid? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var items = await _service.GetFilteredAsync(
            new WarehouseFilterRequest(branchId, search, isActive, page, pageSize));
        return Ok(items);
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            if (result is null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            return Ok(await _service.SetDefaultAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }
}
