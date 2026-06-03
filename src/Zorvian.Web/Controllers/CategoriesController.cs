using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _service.GetAllAsync();
        return Ok(categories);
    }

    [Audit("Category", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), null, category);
    }

    [Audit("Category", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _service.UpdateAsync(id, request);
        if (category is null)
            return NotFound(new { error = "Category not found" });
        return Ok(category);
    }

    [Audit("Category", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Category not found" });
        return NoContent();
    }
}
