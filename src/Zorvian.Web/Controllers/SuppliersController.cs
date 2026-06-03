using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/suppliers")]
public sealed class SuppliersController : ControllerBase
{
    private readonly SupplierService _service;

    public SuppliersController(SupplierService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var suppliers = await _service.GetAllAsync();
        return Ok(suppliers);
    }

    [Audit("Supplier", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request)
    {
        var supplier = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, supplier);
    }

    [Audit("Supplier", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request)
    {
        var supplier = await _service.UpdateAsync(id, request);
        if (supplier is null)
            return NotFound(new { error = "Supplier not found" });
        return Ok(supplier);
    }

    [Audit("Supplier", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Supplier not found" });
        return NoContent();
    }
}
