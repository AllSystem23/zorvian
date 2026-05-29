using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Department;
using Nexora.Application.Services;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _service;

    public DepartmentsController(DepartmentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dept = await _service.GetByIdAsync(id);
        if (dept is null)
            return NotFound(new { error = "Department not found" });
        return Ok(dept);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _service.GetAllAsync();
        return Ok(departments);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var dept = await _service.UpdateAsync(id, request);
        if (dept is null)
            return NotFound(new { error = "Department not found" });
        return Ok(dept);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, error) = await _service.DeleteAsync(id);
        if (!success)
            return error == "Department not found"
                ? NotFound(new { error })
                : Conflict(new { error });
        return NoContent();
    }
}
