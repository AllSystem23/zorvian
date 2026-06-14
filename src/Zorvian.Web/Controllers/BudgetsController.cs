using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/budgets")]
public sealed class BudgetsController : ControllerBase
{
    private readonly BudgetService _service;

    public BudgetsController(BudgetService service)
    {
        _service = service;
    }

    [RequirePermission(Permissions.AccountingRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? year, [FromQuery] int? month)
    {
        if (year.HasValue && month.HasValue)
        {
            var items = await _service.GetByPeriodAsync(year.Value, month.Value);
            return Ok(items);
        }
        var all = await _service.GetAllAsync();
        return Ok(all);
    }

    [Audit("Budget", "Create")]
    [RequirePermission(Permissions.AccountingWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, item);
    }

    [Audit("Budget", "Update")]
    [RequirePermission(Permissions.AccountingWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null)
            return NotFound(new { error = "Budget not found" });
        return Ok(item);
    }

    [Audit("Budget", "Delete")]
    [RequirePermission(Permissions.AccountingWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Budget not found" });
        return NoContent();
    }
}
