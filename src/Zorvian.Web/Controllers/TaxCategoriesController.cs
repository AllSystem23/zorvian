using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/tax-categories")]
public sealed class TaxCategoriesController : ControllerBase
{
    private readonly TaxCategoryService _service;

    public TaxCategoriesController(TaxCategoryService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Tax category not found" });
        return Ok(item);
    }

    [Audit("TaxCategory", "Create")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaxCategoryRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("TaxCategory", "Update")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxCategoryRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Tax category not found" });
        return Ok(item);
    }

    [Audit("TaxCategory", "Delete")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Tax category not found" });
        return NoContent();
    }
}
