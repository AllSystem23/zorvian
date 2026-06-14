using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Partner;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class PartnerController : ControllerBase
{
    private readonly IPartnerService _service;

    public PartnerController(IPartnerService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.SaleWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePartnerRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.SaleWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartnerRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [RequirePermission(Permissions.SaleRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound(new { error = "Partner no encontrado" });
        return Ok(result);
    }

    [RequirePermission(Permissions.SaleRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? countryCode,
        [FromQuery] string? partnerType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var results = await _service.GetFilteredAsync(search, status, countryCode, partnerType, page, pageSize);
        var total = await _service.GetFilteredCountAsync(search, status, countryCode, partnerType);
        return Ok(new { items = results, total, page, pageSize });
    }

    [RequirePermission(Permissions.SaleWrite)]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await _service.ActivateAsync(id);
        return Ok(result);
    }

    [RequirePermission(Permissions.SaleWrite)]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivatePartnerRequest request)
    {
        var result = await _service.DeactivateAsync(id, request.Reason);
        return Ok(result);
    }

    [RequirePermission(Permissions.SaleRead)]
    [HttpGet("by-country/{countryCode}")]
    public async Task<IActionResult> GetActiveByCountry(string countryCode)
    {
        var results = await _service.GetActiveByCountryAsync(countryCode);
        return Ok(results);
    }
}

public sealed record DeactivatePartnerRequest(string Reason);
