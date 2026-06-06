using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranty-costs")]
public sealed class WarrantyCostsController : ControllerBase
{
    private readonly WarrantyCostService _service;

    public WarrantyCostsController(WarrantyCostService service) => _service = service;

    [HttpGet("by-warranty/{warrantyId:guid}")]
    public async Task<IActionResult> GetByWarranty(Guid warrantyId)
    {
        var items = await _service.GetByWarrantyIdAsync(warrantyId);
        return Ok(items);
    }

    [HttpGet("by-claim/{claimId:guid}")]
    public async Task<IActionResult> GetByClaim(Guid claimId)
    {
        var items = await _service.GetByClaimIdAsync(claimId);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { error = "Cost not found" });
        return Ok(item);
    }

    [Audit("WarrantyCost", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyCostRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("WarrantyCost", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarrantyCostRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null)
            return NotFound(new { error = "Cost not found" });
        return Ok(item);
    }

    [Audit("WarrantyCost", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Cost not found" });
        return NoContent();
    }
}
