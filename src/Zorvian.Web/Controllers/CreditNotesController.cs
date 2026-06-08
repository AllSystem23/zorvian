using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/credit-notes")]
public sealed class CreditNotesController : ControllerBase
{
    private readonly CreditNoteService _service;

    public CreditNotesController(CreditNoteService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { error = "Credit note not found" });
        return Ok(item);
    }

    [HttpGet("by-sale/{saleId:guid}")]
    public async Task<IActionResult> GetBySaleId(Guid saleId)
    {
        var items = await _service.GetBySaleIdAsync(saleId);
        return Ok(items);
    }

    [Audit("CreditNote", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteRequest request)
    {
        try
        {
            var item = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
