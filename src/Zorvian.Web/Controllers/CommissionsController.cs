using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/commissions")]
public sealed class CommissionsController : ControllerBase
{
    private readonly CommissionService _service;

    public CommissionsController(CommissionService service) => _service = service;

    [Audit("Commission", "ReadSchemes")]
    [HttpGet("schemes")]
    [RequirePermission(Permissions.CommissionRead)]
    public async Task<IActionResult> GetSchemes() =>
        Ok(await _service.GetSchemesAsync());

    [Audit("Commission", "CreateScheme")]
    [HttpPost("schemes")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> CreateScheme([FromBody] CommissionScheme scheme)
    {
        var result = await _service.CreateSchemeAsync(scheme);
        return CreatedAtAction(nameof(GetSchemeById), new { id = result.Id }, result);
    }

    [Audit("Commission", "ReadScheme")]
    [HttpGet("schemes/{id:guid}")]
    [RequirePermission(Permissions.CommissionRead)]
    public async Task<IActionResult> GetSchemeById(Guid id)
    {
        var result = await _service.GetSchemeByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Commission", "UpdateScheme")]
    [HttpPut("schemes/{id:guid}")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> UpdateScheme(Guid id, [FromBody] CommissionScheme scheme)
    {
        var result = await _service.UpdateSchemeAsync(id, scheme);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Commission", "DeleteScheme")]
    [HttpDelete("schemes/{id:guid}")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> DeleteScheme(Guid id) =>
        await _service.DeleteSchemeAsync(id) ? NoContent() : NotFound();

    [Audit("Commission", "ReadRules")]
    [HttpGet("schemes/{id:guid}/rules")]
    [RequirePermission(Permissions.CommissionRead)]
    public async Task<IActionResult> GetRulesByScheme(Guid id) =>
        Ok(await _service.GetRulesBySchemeAsync(id));

    [HttpPost("schemes/{id:guid}/rules")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> AddRule(Guid id, [FromBody] CommissionRule rule)
    {
        rule.CommissionSchemeId = id;
        var result = await _service.AddRuleAsync(rule);
        return CreatedAtAction(nameof(GetRulesByScheme), new { id }, result);
    }

    [HttpPut("rules/{id:guid}")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] CommissionRule rule)
    {
        rule.Id = id;
        await _service.UpdateRuleAsync(rule);
        return NoContent();
    }

    [HttpDelete("rules/{id:guid}")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> DeleteRule(Guid id) =>
        await _service.DeleteRuleAsync(id) ? NoContent() : NotFound();

    [Audit("Commission", "ReadAssignments")]
    [HttpGet("assignments")]
    [RequirePermission(Permissions.CommissionRead)]
    public async Task<IActionResult> GetAssignments([FromQuery] Guid? schemeId, [FromQuery] Guid? employeeId)
    {
        if (schemeId.HasValue)
            return Ok(await _service.GetAssignmentsBySchemeAsync(schemeId.Value));
        if (employeeId.HasValue)
            return Ok(await _service.GetAssignmentsByEmployeeAsync(employeeId.Value));
        return Ok(new List<CommissionAssignment>());
    }

    [Audit("Commission", "AssignEmployee")]
    [HttpPost("assignments")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> AssignEmployee([FromBody] CommissionAssignment assignment)
    {
        var result = await _service.AssignEmployeeAsync(assignment);
        return CreatedAtAction(nameof(GetAssignments), new { schemeId = result.CommissionSchemeId }, result);
    }

    [HttpDelete("assignments/{id:guid}")]
    [RequirePermission(Permissions.CommissionWrite)]
    public async Task<IActionResult> RemoveAssignment(Guid id) =>
        await _service.UnassignEmployeeAsync(id) ? NoContent() : NotFound();

    [Audit("Commission", "Calculate")]
    [HttpPost("calculate/{periodId:guid}")]
    [RequirePermission(Permissions.CommissionProcess)]
    public async Task<IActionResult> CalculateCommissions(Guid periodId)
    {
        var result = await _service.CalculateCommissionsAsync(periodId);
        return Ok(result);
    }

    [Audit("Commission", "ReadRecords")]
    [HttpGet("records")]
    [RequirePermission(Permissions.CommissionRead)]
    public async Task<IActionResult> GetRecords([FromQuery] Guid? periodId, [FromQuery] Guid? employeeId)
    {
        if (periodId is null && employeeId is null)
            return Ok(new List<CommissionRecord>());

        if (periodId.HasValue)
        {
            var records = await _service.GetCommissionRecordsByPeriodAsync(periodId.Value);
            if (employeeId.HasValue)
                records = records.Where(r => r.EmployeeId == employeeId.Value).ToList();
            return Ok(records);
        }

        return Ok(new List<CommissionRecord>());
    }

    [Audit("Commission", "ApproveRecord")]
    [HttpPost("records/{id:guid}/approve")]
    [RequirePermission(Permissions.CommissionProcess)]
    public async Task<IActionResult> ApproveRecord(Guid id) =>
        await _service.ApproveCommissionAsync(id) ? Ok(new { status = "approved" }) : NotFound();
}
