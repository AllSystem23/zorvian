using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranty-part-requests")]
public sealed class WarrantyPartRequestsController : ControllerBase
{
    private readonly WarrantyPartRequestService _service;

    public WarrantyPartRequestsController(WarrantyPartRequestService service) => _service = service;

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("by-claim/{claimId:guid}")]
    public async Task<IActionResult> GetByClaim(Guid claimId)
    {
        var items = await _service.GetByClaimIdAsync(claimId);
        return Ok(items);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("by-provider/{providerId:guid}")]
    public async Task<IActionResult> GetByProvider(Guid providerId)
    {
        var items = await _service.GetByProviderIdAsync(providerId);
        return Ok(items);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { error = "Part request not found" });
        return Ok(item);
    }

    [Audit("WarrantyPartRequest", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyPartRequestRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("WarrantyPartRequest", "UpdateStatus")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWarrantyPartRequestStatusRequest request)
    {
        var item = await _service.UpdateStatusAsync(id, request);
        if (item is null)
            return NotFound(new { error = "Part request not found" });
        return Ok(item);
    }
}
