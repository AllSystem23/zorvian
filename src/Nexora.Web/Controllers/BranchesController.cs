using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Branch;
using Nexora.Application.Services;
using Nexora.Web.Filters;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/branches")]
public sealed class BranchesController : ControllerBase
{
    private readonly BranchService _service;

    public BranchesController(BranchService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var branches = await _service.GetAllAsync();
        return Ok(branches);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var branch = await _service.GetByIdAsync(id);
        if (branch is null)
            return NotFound(new { error = "Branch not found" });
        return Ok(branch);
    }

    [Audit("Branch", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request)
    {
        var branch = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
    }

    [Audit("Branch", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchRequest request)
    {
        var branch = await _service.UpdateAsync(id, request);
        if (branch is null)
            return NotFound(new { error = "Branch not found" });
        return Ok(branch);
    }

    [Audit("Branch", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Branch not found" });
        return NoContent();
    }
}
