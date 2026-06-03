using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Performance;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/performance")]
public sealed class PerformanceController : ControllerBase
{
    private readonly PerformanceService _service;

    public PerformanceController(PerformanceService service)
    {
        _service = service;
    }

    [HttpGet("objectives/{employeeId:guid}")]
    public async Task<IActionResult> GetObjectives(Guid employeeId)
    {
        return Ok(await _service.GetObjectivesAsync(employeeId));
    }

    [HttpPost("objectives")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> CreateObjective([FromBody] CreateObjectiveRequest request)
    {
        return Ok(await _service.CreateObjectiveAsync(request));
    }

    [HttpPost("objectives/{objectiveId:guid}/key-results")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> AddKeyResult(Guid objectiveId, [FromBody] CreateKeyResultRequest request)
    {
        return Ok(await _service.AddKeyResultAsync(objectiveId, request));
    }

    [HttpPut("key-results/{id:guid}")]
    public async Task<IActionResult> UpdateKeyResult(Guid id, [FromBody] UpdateKeyResultRequest request)
    {
        try
        {
            return Ok(await _service.UpdateKeyResultAsync(id, request));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
