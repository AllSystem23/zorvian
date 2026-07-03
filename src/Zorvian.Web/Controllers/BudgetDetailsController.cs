using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/budget-details")]
public sealed class BudgetDetailsController : ControllerBase
{
    private readonly BudgetDetailService _service;

    public BudgetDetailsController(BudgetDetailService service) => _service = service;

    [RequirePermission(Permissions.AccountingRead)]
    [HttpGet("by-budget/{budgetId:guid}")]
    public async Task<IActionResult> GetByBudgetId(Guid budgetId)
    {
        var result = await _service.GetByBudgetIdAsync(budgetId);
        return Ok(result);
    }

    [RequirePermission(Permissions.AccountingRead)]
    [HttpGet("by-period")]
    public async Task<IActionResult> GetByPeriod([FromQuery] int year, [FromQuery] int month)
    {
        var result = await _service.GetByPeriodAsync(year, month);
        return Ok(result);
    }

    [RequirePermission(Permissions.AccountingWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDetailRequest request)
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

    [RequirePermission(Permissions.AccountingWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDetailRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        if (result is null) return NotFound(new { error = "Budget detail not found" });
        return Ok(result);
    }

    [RequirePermission(Permissions.AccountingWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(new { error = "Budget detail not found" });
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("zorvian/v1/budget-tracking")]
public sealed class BudgetTrackingController : ControllerBase
{
    private readonly BudgetTrackingService _service;

    public BudgetTrackingController(BudgetTrackingService service) => _service = service;

    [RequirePermission(Permissions.AccountingRead)]
    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] BudgetTrackingFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [RequirePermission(Permissions.AccountingWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetTrackingRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
