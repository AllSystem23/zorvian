using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly BrandService _service;

    public BrandsController(BrandService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await _service.GetAllAsync();
        return Ok(brands);
    }

    [Audit("Brand", "Create")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBrandRequest request)
    {
        var brand = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, brand);
    }

    [Audit("Brand", "Update")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBrandRequest request)
    {
        var brand = await _service.UpdateAsync(id, request);
        if (brand is null)
            return NotFound(new { error = "Brand not found" });
        return Ok(brand);
    }

    [Audit("Brand", "Delete")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Brand not found" });
        return NoContent();
    }
}
