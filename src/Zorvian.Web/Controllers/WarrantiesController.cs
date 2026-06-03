using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/warranties")]
public sealed class WarrantiesController : ControllerBase
{
    private readonly WarrantyService _service;

    public WarrantiesController(WarrantyService service)
    {
        _service = service;
    }

    [Audit("Warranty", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
    {
        var warranty = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = warranty.Id }, warranty);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var warranty = await _service.GetByIdAsync(id);
        if (warranty is null)
            return NotFound(new { error = "Warranty not found" });
        return Ok(warranty);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] WarrantyFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("WarrantyClaim", "Create")]
    [HttpPost("{id:guid}/claims")]
    public async Task<IActionResult> AddClaim(Guid id, [FromBody] CreateWarrantyClaimRequest request)
    {
        try
        {
            var claim = await _service.AddClaimAsync(request with { WarrantyId = id });
            return Ok(claim);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
