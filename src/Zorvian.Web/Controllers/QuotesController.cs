using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/quotes")]
public sealed class QuotesController : ControllerBase
{
    private readonly QuoteService _service;

    public QuotesController(QuoteService service)
    {
        _service = service;
    }

    [Audit("Quote", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request)
    {
        try
        {
            var quote = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = quote.Id }, quote);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var quote = await _service.GetByIdAsync(id);
        if (quote is null)
            return NotFound(new { error = "Quote not found" });
        return Ok(quote);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] QuoteFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Quote", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuoteRequest request)
    {
        var quote = await _service.UpdateAsync(id, request);
        if (quote is null)
            return NotFound(new { error = "Quote not found" });
        return Ok(quote);
    }

    [Audit("Quote", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Quote not found" });
        return NoContent();
    }
}
