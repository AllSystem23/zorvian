using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/approval-flows")]
public sealed class ApprovalFlowConfigsController : ControllerBase
{
    private readonly ApprovalFlowConfigService _service;
    public ApprovalFlowConfigsController(ApprovalFlowConfigService service) => _service = service;

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
        if (item is null) return NotFound(new { error = "Flow config not found" });
        return Ok(item);
    }

    [Audit("ApprovalFlowConfig", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApprovalFlowConfigRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("ApprovalFlowConfig", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApprovalFlowConfigRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Flow config not found" });
        return Ok(item);
    }

    [Audit("ApprovalFlowConfig", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Flow config not found" });
        return NoContent();
    }
}
