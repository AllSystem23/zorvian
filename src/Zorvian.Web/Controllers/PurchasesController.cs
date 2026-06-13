using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/purchases")]
public sealed class PurchasesController : ControllerBase
{
    private readonly PurchaseService _service;
    private readonly IPurchaseIntelligenceService _aiService;

    public PurchasesController(PurchaseService service, IPurchaseIntelligenceService aiService)
    {
        _service = service;
        _aiService = aiService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeInvoice(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { error = "No file uploaded" });

        using var stream = file.OpenReadStream();
        var result = await _aiService.AnalyzeInvoiceAsync(stream);
        return Ok(result);
    }

    [Audit("Purchase", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request)
    {
        try
        {
            var purchase = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var purchase = await _service.GetByIdAsync(id);
        if (purchase is null)
            return NotFound(new { error = "Purchase not found" });
        return Ok(purchase);
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] PurchaseFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Purchase", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseRequest request)
    {
        try
        {
            var purchase = await _service.UpdateAsync(id, request);
            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("Purchase", "Complete")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _service.CompleteAsync(id);
        return NoContent();
    }

    [Audit("Purchase", "Cancel")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var purchase = await _service.CancelAsync(id);
            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("aging")]
    public async Task<IActionResult> GetAging()
    {
        var result = await _service.GetAgingAsync();
        return Ok(result);
    }
}
