using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/goals")]
public sealed class GoalsController : ControllerBase
{
    private readonly GoalService _service;

    public GoalsController(GoalService service) => _service = service;

    [Audit("Goal", "ReadDefinitions")]
    [HttpGet("definitions")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetDefinitions() =>
        Ok(await _service.GetGoalDefinitionsAsync());

    [Audit("Goal", "ReadDefinition")]
    [HttpGet("definitions/{id:guid}")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetDefinitionById(Guid id)
    {
        var result = await _service.GetGoalDefinitionByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Goal", "CreateDefinition")]
    [HttpPost("definitions")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> CreateDefinition([FromBody] GoalDefinition definition)
    {
        var result = await _service.CreateGoalDefinitionAsync(definition);
        return CreatedAtAction(nameof(GetDefinitionById), new { id = result.Id }, result);
    }

    [Audit("Goal", "UpdateDefinition")]
    [HttpPut("definitions/{id:guid}")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> UpdateDefinition(Guid id, [FromBody] GoalDefinition definition)
    {
        var result = await _service.UpdateGoalDefinitionAsync(id, definition);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Goal", "DeleteDefinition")]
    [HttpDelete("definitions/{id:guid}")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> DeleteDefinition(Guid id) =>
        await _service.DeleteGoalDefinitionAsync(id) ? NoContent() : NotFound();

    [Audit("Goal", "Assign")]
    [HttpPost("assign")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> AssignGoal([FromBody] GoalAssignment assignment)
    {
        var result = await _service.AssignGoalAsync(assignment);
        return CreatedAtAction(nameof(GetAssignments), new { employeeId = result.EmployeeId }, result);
    }

    [Audit("Goal", "ReadAssignments")]
    [HttpGet("assignments")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetAssignments([FromQuery] Guid? employeeId, [FromQuery] Guid? goalId)
    {
        if (employeeId.HasValue)
            return Ok(await _service.GetAssignmentsByEmployeeAsync(employeeId.Value));
        if (goalId.HasValue)
            return Ok(await _service.GetAssignmentsByGoalAsync(goalId.Value));
        return Ok(new List<GoalAssignment>());
    }

    [Audit("Goal", "RecordProgress")]
    [HttpPost("progress")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> RecordProgress([FromBody] GoalProgress progress)
    {
        var result = await _service.RecordProgressAsync(progress);
        return Ok(result);
    }

    [Audit("Goal", "Evaluate")]
    [HttpPost("evaluate/{assignmentId:guid}")]
    [RequirePermission(Permissions.GoalEvaluate)]
    public async Task<IActionResult> EvaluateGoal(Guid assignmentId)
    {
        var result = await _service.EvaluateGoalAsync(assignmentId);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Goal", "ReadIncentives")]
    [HttpGet("incentives")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetIncentives() =>
        Ok(await _service.GetIncentivesAsync());

    [Audit("Goal", "ReadIncentive")]
    [HttpGet("incentives/{id:guid}")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetIncentiveById(Guid id)
    {
        var result = await _service.GetIncentiveByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Goal", "CreateIncentive")]
    [HttpPost("incentives")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> CreateIncentive([FromBody] Incentive incentive)
    {
        var result = await _service.CreateIncentiveAsync(incentive);
        return CreatedAtAction(nameof(GetIncentiveById), new { id = result.Id }, result);
    }

    [Audit("Goal", "UpdateIncentive")]
    [HttpPut("incentives/{id:guid}")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> UpdateIncentive(Guid id, [FromBody] Incentive incentive)
    {
        var result = await _service.UpdateIncentiveAsync(id, incentive);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Goal", "DeleteIncentive")]
    [HttpDelete("incentives/{id:guid}")]
    [RequirePermission(Permissions.GoalWrite)]
    public async Task<IActionResult> DeleteIncentive(Guid id) =>
        await _service.DeleteIncentiveAsync(id) ? NoContent() : NotFound();

    [Audit("Goal", "ReadIncentivePayments")]
    [HttpGet("incentive-payments")]
    [RequirePermission(Permissions.GoalRead)]
    public async Task<IActionResult> GetIncentivePayments([FromQuery] Guid? employeeId, [FromQuery] string? status)
    {
        if (employeeId.HasValue)
        {
            var all = await _service.GetIncentivePaymentsAsync(employeeId.Value);
            if (!string.IsNullOrEmpty(status))
                all = all.Where(p => p.Status == status).ToList();
            return Ok(all);
        }
        return Ok(new List<IncentivePayment>());
    }

    [Audit("Goal", "ApproveIncentivePayment")]
    [HttpPost("incentive-payments/{id:guid}/approve")]
    [RequirePermission(Permissions.GoalEvaluate)]
    public async Task<IActionResult> ApproveIncentivePayment(Guid id)
    {
        var result = await _service.ApproveIncentivePaymentAsync(id);
        return Ok(result);
    }
}
