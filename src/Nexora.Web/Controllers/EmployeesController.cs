using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Employee;
using Nexora.Application.Services;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly EmployeeService _service;

    public EmployeesController(EmployeeService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        var employee = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _service.GetByIdAsync(id);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] EmployeeFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _service.UpdateAsync(id, request);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Employee not found" });
        return NoContent();
    }
}
