using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/payroll")]
public sealed class PayrollController : ControllerBase
{
    private readonly PayrollService _service;
    private readonly IDocumentTemplateRepository _documentTemplateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        PayrollService service,
        IDocumentTemplateRepository documentTemplateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<PayrollController> logger)
    {
        _service = service;
        _documentTemplateRepo = documentTemplateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    // Deduction Types
    [Audit("Payroll", "ReadDeductionTypes")]
    [HttpGet("deduction-types")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetDeductionTypes() =>
        Ok(await _service.GetDeductionTypesAsync());

    [HttpPost("deduction-types")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CreateDeductionType([FromBody] CreateDeductionTypeRequest request)
    {
        var result = await _service.CreateDeductionTypeAsync(request);
        return CreatedAtAction(nameof(GetDeductionTypes), null, result);
    }

    [HttpPut("deduction-types/{id:guid}")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> UpdateDeductionType(Guid id, [FromBody] UpdateDeductionTypeRequest request)
    {
        var result = await _service.UpdateDeductionTypeAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("deduction-types/{id:guid}")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> DeleteDeductionType(Guid id) =>
        await _service.DeleteDeductionTypeAsync(id) ? NoContent() : NotFound();

    // Employee Salaries
    [Audit("Payroll", "ReadSalaries")]
    [HttpGet("salaries")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetSalaries([FromQuery] Guid? employeeId) =>
        Ok(await _service.GetSalariesAsync(employeeId));

    [HttpPost("salaries")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CreateSalary([FromBody] CreateEmployeeSalaryRequest request)
    {
        var result = await _service.CreateSalaryAsync(request);
        return CreatedAtAction(nameof(GetSalaries), new { employeeId = request.EmployeeId }, result);
    }

    [HttpDelete("salaries/{id:guid}")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> DeactivateSalary(Guid id) =>
        await _service.DeactivateSalaryAsync(id) ? NoContent() : NotFound();

    // Payroll Periods
    [Audit("Payroll", "ReadPeriods")]
    [HttpGet("periods")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetPeriods([FromQuery] int? year) =>
        Ok(await _service.GetPeriodsAsync(year));

    [HttpPost("periods")]
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> CreatePeriod([FromBody] CreatePayrollPeriodRequest request)
    {
        var result = await _service.CreatePeriodAsync(request);
        return CreatedAtAction(nameof(GetPeriods), new { year = result.Year }, result);
    }

    // Payroll Runs
    [Audit("Payroll", "ReadRuns")]
    [HttpGet("runs")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetRuns([FromQuery] Guid? periodId) =>
        Ok(await _service.GetRunsAsync(periodId));

    [Audit("Payroll", "ReadRun")]
    [HttpGet("runs/{id:guid}")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GetRunById(Guid id)
    {
        var result = await _service.GetRunByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("runs/generate")]
    [RequirePermission(Permissions.PayrollProcess)]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequest request)
    {
        try
        {
            var result = await _service.GeneratePayrollAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("runs/{id:guid}/approve")]
    [RequirePermission(Permissions.PayrollProcess)]
    public async Task<IActionResult> ApproveRun(Guid id)
    {
        var result = await _service.ApproveRunAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("runs/{id:guid}")]
    [RequirePermission(Permissions.PayrollWrite)]
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
    [RequirePermission(Permissions.PayrollWrite)]
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
    [RequirePermission(Permissions.PayrollProcess)]
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
    [RequirePermission(Permissions.PayrollWrite)]
    public async Task<IActionResult> UpdateDetail(Guid detailId, [FromBody] UpdatePayrollDetailRequest request)
    {
        var result = await _service.UpdateDetailAsync(detailId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [Audit("Payroll", "ExportAch")]
    [HttpGet("runs/{id:guid}/export-ach")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> ExportAch(Guid id)
    {
        var result = await _service.ExportAchFileAsync(id);
        if (result is null) return NotFound(new { message = "Payroll run not found or not approved" });

        return File(result.Content, "text/csv", result.FileName);
    }

    [Audit("Payroll", "GeneratePayrollSheet")]
    [HttpPost("runs/{id:guid}/pdf/generate")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GeneratePayrollSheet(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("payroll", id);
            if (context is null) return NotFound(new { message = "Corrida de nómina no encontrada" });

            var template = await _documentTemplateRepo.GetByNameAsync("Planilla de Pago");
            if (template is null) return NotFound(new { message = "Plantilla 'Planilla de Pago' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "payroll");

            return Ok(new
            {
                generatedDocumentId = generated.Id,
                name = generated.Name,
                status = generated.Status,
                createdAt = generated.CreatedAt,
                downloadUrl = $"/zorvian/v1/documents/{generated.Id}/pdf"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando planilla de pago para corrida {RunId}", id);
            return StatusCode(500, new { message = "Error interno al generar la planilla de pago" });
        }
    }

    [Audit("Payroll", "GeneratePayslip")]
    [HttpPost("details/{detailId:guid}/pdf/generate")]
    [RequirePermission(Permissions.PayrollRead)]
    public async Task<IActionResult> GeneratePayslip(Guid detailId)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("payrolldetail", detailId);
            if (context is null) return NotFound(new { message = "Detalle de nómina no encontrado" });

            var template = await _documentTemplateRepo.GetByNameAsync("Colilla de Pago");
            if (template is null) return NotFound(new { message = "Plantilla 'Colilla de Pago' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                detailId,
                context.Data,
                "payrolldetail");

            return Ok(new
            {
                generatedDocumentId = generated.Id,
                name = generated.Name,
                status = generated.Status,
                createdAt = generated.CreatedAt,
                downloadUrl = $"/zorvian/v1/documents/{generated.Id}/pdf"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando colilla de pago para detalle {DetailId}", detailId);
            return StatusCode(500, new { message = "Error interno al generar la colilla de pago" });
        }
    }
}
