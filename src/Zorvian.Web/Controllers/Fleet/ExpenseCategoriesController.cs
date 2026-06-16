using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/expense-categories")]
public sealed class ExpenseCategoriesController : ControllerBase
{
    private readonly ExpenseCategoryService _service;

    public ExpenseCategoriesController(ExpenseCategoryService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Expense category not found" });
        return Ok(item);
    }

    [Audit("FleetExpenseCategory", "Create")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseCategoryRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("FleetExpenseCategory", "Update")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseCategoryRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Expense category not found" });
        return Ok(item);
    }

    [Audit("FleetExpenseCategory", "Delete")]
    [RequirePermission(Permissions.FleetConfig)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Expense category not found" });
        return NoContent();
    }
}
