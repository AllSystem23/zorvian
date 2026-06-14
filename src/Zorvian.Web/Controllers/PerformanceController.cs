using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Performance;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/performance")]
public sealed class PerformanceController : ControllerBase
{
    private readonly PerformanceService _service;

    public PerformanceController(PerformanceService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.KpiRead)]
    [HttpGet("objectives/{employeeId:guid}")]
    public async Task<IActionResult> GetObjectives(Guid employeeId)
    {
        return Ok(await _service.GetObjectivesAsync(employeeId));
    }

    [HttpPost("objectives")]
    [RequirePermission(Permissions.KpiWrite)]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> CreateObjective([FromBody] CreateObjectiveRequest request)
    {
        return Ok(await _service.CreateObjectiveAsync(request));
    }

    [HttpPost("objectives/{objectiveId:guid}/key-results")]
    [RequirePermission(Permissions.KpiWrite)]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> AddKeyResult(Guid objectiveId, [FromBody] CreateKeyResultRequest request)
    {
        return Ok(await _service.AddKeyResultAsync(objectiveId, request));
    }

    [RequirePermission(Permissions.KpiWrite)]
    [HttpPut("key-results/{id:guid}")]
    public async Task<IActionResult> UpdateKeyResult(Guid id, [FromBody] UpdateKeyResultRequest request)
    {
        return Ok(await _service.UpdateKeyResultAsync(id, request));
    }
}
