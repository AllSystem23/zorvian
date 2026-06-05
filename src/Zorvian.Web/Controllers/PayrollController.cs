using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/payroll")]
public sealed class PayrollController : ControllerBase
{
    private readonly PayrollService _service;

    public PayrollController(PayrollService service) => _service = service;

    // Deduction Types
    [HttpGet("deduction-types")]
    public async Task<IActionResult> GetDeductionTypes() =>
        Ok(await _service.GetDeductionTypesAsync());

    [HttpPost("deduction-types")]
    public async Task<IActionResult> CreateDeductionType([FromBody] CreateDeductionTypeRequest request)
    {
        var result = await _service.CreateDeductionTypeAsync(request);
        return CreatedAtAction(nameof(GetDeductionTypes), null, result);
    }

    [HttpPut("deduction-types/{id:guid}")]
    public async Task<IActionResult> UpdateDeductionType(Guid id, [FromBody] UpdateDeductionTypeRequest request)
    {
        var result = await _service.UpdateDeductionTypeAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("deduction-types/{id:guid}")]
    public async Task<IActionResult> DeleteDeductionType(Guid id) =>
        await _service.DeleteDeductionTypeAsync(id) ? NoContent() : NotFound();

    // Employee Salaries
    [HttpGet("salaries")]
    public async Task<IActionResult> GetSalaries([FromQuery] Guid? employeeId) =>
        Ok(await _service.GetSalariesAsync(employeeId));

    [HttpPost("salaries")]
    public async Task<IActionResult> CreateSalary([FromBody] CreateEmployeeSalaryRequest request)
    {
        var result = await _service.CreateSalaryAsync(request);
        return CreatedAtAction(nameof(GetSalaries), new { employeeId = request.EmployeeId }, result);
    }

    [HttpDelete("salaries/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> DeactivateSalary(Guid id) =>
        await _service.DeactivateSalaryAsync(id) ? NoContent() : NotFound();

    // Payroll Periods
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods([FromQuery] int? year) =>
        Ok(await _service.GetPeriodsAsync(year));

    [HttpPost("periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        var result = await _service.CreatePeriodAsync(request);
        return CreatedAtAction(nameof(GetPeriods), new { year = result.Year }, result);
    }

    // Payroll Runs
    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns([FromQuery] Guid? periodId) =>
        Ok(await _service.GetRunsAsync(periodId));

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetRunById(Guid id)
    {
        var result = await _service.GetRunByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("runs/generate")]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequest request)
    {
        try
        {
            var result = await _service.GeneratePayrollAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("runs/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRun(Guid id)
    {
        var result = await _service.ApproveRunAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("runs/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> DeleteRun(Guid id)
    {
        try
        {
            return await _service.DeleteRunAsync(id) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("runs/{id:guid}/cancel")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> CancelRun(Guid id)
    {
        try
        {
            var result = await _service.CancelRunAsync(id);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("runs/{id:guid}/mark-paid")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> MarkAsPaid(Guid id)
    {
        try
        {
            var result = await _service.MarkAsPaidAsync(id);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("details/{detailId:guid}")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> UpdateDetail(Guid detailId, [FromBody] UpdatePayrollDetailRequest request)
    {
        var result = await _service.UpdateDetailAsync(detailId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("runs/{id:guid}/export-ach")]
    public async Task<IActionResult> ExportAch(Guid id)
    {
        var result = await _service.ExportAchFileAsync(id);
        if (result is null) return NotFound(new { message = "Payroll run not found or not approved" });

        return File(result.Content, "text/csv", result.FileName);
    }
}
