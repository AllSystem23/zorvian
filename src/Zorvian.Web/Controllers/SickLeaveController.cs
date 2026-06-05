using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class SickLeaveController : ControllerBase
{
    private readonly SickLeaveService _service;

    public SickLeaveController(SickLeaveService service) => _service = service;

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId) =>
        Ok(await _service.GetByEmployeeAsync(employeeId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSickLeaveRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result != null ? Ok(result) : BadRequest();
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _service.ApproveAsync(id);
        return result ? NoContent() : BadRequest();
    }
}
