using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/accounting-entries")]
public sealed class AccountingEntriesController : ControllerBase
{
    private readonly AccountingEntryService _service;
    public AccountingEntriesController(AccountingEntryService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualEntryRequest request)
    {
        try
        {
            var entry = await _service.CreateManualEntryAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entry = await _service.GetByIdAsync(id);
        if (entry is null) return NotFound(new { error = "Entry not found" });
        return Ok(entry);
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] Guid? periodId, [FromQuery] string? referenceType,
        [FromQuery] string? status, [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetFilteredAsync(periodId, referenceType, status, fromDate, toDate, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id)
    {
        try
        {
            var entry = await _service.PostEntryAsync(id);
            return Ok(entry);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
