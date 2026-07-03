using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/employee-bank-accounts")]
public sealed class EmployeeBankAccountsController : ControllerBase
{
    private readonly BankAccountService _service;

    public EmployeeBankAccountsController(BankAccountService service) => _service = service;

    [Audit("BankAccount", "Read")]
    [RequirePermission(Permissions.CashRead)]
    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId) =>
        Ok(await _service.GetByEmployeeIdAsync(employeeId));

    [Audit("BankAccount", "Read")]
    [RequirePermission(Permissions.CashRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }

    [RequirePermission(Permissions.CashWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeBankAccountRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [RequirePermission(Permissions.CashWrite)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeBankAccountRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result != null ? Ok(result) : NotFound();
    }

    [RequirePermission(Permissions.CashWrite)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
