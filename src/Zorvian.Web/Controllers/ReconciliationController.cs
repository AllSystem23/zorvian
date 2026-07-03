using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/reconciliations")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly ReconciliationService _service;

    public ReconciliationController(ReconciliationService service) => _service = service;

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound(new { error = "Reconciliation not found" });
        return Ok(result);
    }

    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetFiltered([FromQuery] ReconciliationFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Create([FromBody] CreateReconciliationRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReconciliationRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result is null) return NotFound(new { error = "Reconciliation not found" });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(new { error = "Reconciliation not found" });
        return NoContent();
    }

    [HttpPost("{id:guid}/import")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> ImportBankStatement(Guid id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _service.ImportBankStatementAsync(id, stream, file.FileName);
            return Ok(new { imported = result.Imported, failed = result.Failed, message = result.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/auto-match")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> RunAutoMatching(Guid id)
    {
        var matched = await _service.RunAutoMatchingAsync(id);
        return Ok(new { matched });
    }

    [HttpPost("{id:guid}/manual-match")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> ManualMatch(Guid id, [FromQuery] Guid bankDetailId, [FromQuery] Guid systemDetailId)
    {
        var ok = await _service.ManualMatchAsync(id, bankDetailId, systemDetailId);
        if (!ok) return BadRequest(new { error = "One or both details not found" });
        return Ok(new { message = "Matched successfully" });
    }
}
