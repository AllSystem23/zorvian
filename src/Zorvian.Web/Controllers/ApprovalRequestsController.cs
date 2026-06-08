using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Application.Services;

using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/approval-requests")]
public sealed class ApprovalRequestsController : ControllerBase
{
    private readonly IApprovalEngine _engine;
    public ApprovalRequestsController(IApprovalEngine engine) => _engine = engine;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var items = await _engine.GetPendingAsync();
        return Ok(items);
    }

    [HttpGet("by-reference/{referenceId:guid}")]
    public async Task<IActionResult> GetByReference(Guid referenceId)
    {
        var items = await _engine.GetByReferenceAsync(referenceId);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _engine.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Request not found" });
        return Ok(item);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalActionRequest request)
    {
        var item = await _engine.ApproveAsync(id, User.Identity?.Name ?? "unknown", request.Comment);
        if (item is null) return BadRequest(new { error = "Cannot approve" });
        return Ok(item);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApprovalActionRequest request)
    {
        var item = await _engine.RejectAsync(id, User.Identity?.Name ?? "unknown", request.Comment);
        if (item is null) return BadRequest(new { error = "Cannot reject" });
        return Ok(item);
    }
}
