using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranties")]
public sealed class WarrantiesController : ControllerBase
{
    private readonly WarrantyService _service;
    private readonly WarrantyTimelineService _timelineService;

    public WarrantiesController(WarrantyService service, WarrantyTimelineService timelineService)
    {
        _service = service;
        _timelineService = timelineService;
    }

    [Audit("Warranty", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
    {
        var warranty = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = warranty.Id }, warranty);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var warranty = await _service.GetByIdAsync(id);
        if (warranty is null)
            return NotFound(new { error = "Warranty not found" });
        return Ok(warranty);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] WarrantyFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("WarrantyClaim", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("{id:guid}/claims")]
    public async Task<IActionResult> AddClaim(Guid id, [FromBody] CreateWarrantyClaimRequest request)
    {
        var claim = await _service.AddClaimAsync(request with { WarrantyId = id });
        return Ok(claim);
    }

    [Audit("WarrantyClaim", "AssignWorkshop")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("claims/{claimId:guid}/assign-workshop")]
    public async Task<IActionResult> AssignWorkshop(Guid claimId, [FromBody] AssignWorkshopRequest request)
    {
        var claim = await _service.AssignWorkshopAsync(claimId, request);
        return Ok(claim);
    }

    [Audit("WarrantyClaim", "ReferToProvider")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("claims/{claimId:guid}/refer-to-provider")]
    public async Task<IActionResult> ReferToProvider(Guid claimId, [FromBody] ReferToProviderRequest request)
    {
        var claim = await _service.ReferToProviderAsync(claimId, request);
        return Ok(claim);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id)
    {
        return Ok(await _timelineService.GetTimelineAsync(id));
    }
}
