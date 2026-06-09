using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("zorvian/v1/custom-reports")]
[Authorize]
public sealed class CustomReportsController : ControllerBase
{
    private readonly ICustomReportService _service;
    private readonly IReportExportService _export;

    public CustomReportsController(ICustomReportService service, IReportExportService export)
    {
        _service = service;
        _export = export;
    }

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<CustomReportResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync(CurrentUserId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomReportResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CustomReportResponse>> Create(CreateCustomReportRequest request)
    {
        var result = await _service.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomReportResponse>> Update(Guid id, UpdateCustomReportRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/execute")]
    public async Task<ActionResult<ReportResult>> Execute(Guid id)
    {
        try
        {
            var result = await _service.ExecuteAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("execute-adhoc")]
    public async Task<ActionResult<ReportResult>> ExecuteAdHoc(
        [FromQuery] string module,
        [FromBody] CreateCustomReportRequest request)
    {
        var result = await _service.ExecuteAdHocAsync(module, request);
        return Ok(result);
    }

    [HttpPost("{id:guid}/export/excel")]
    public async Task<ActionResult> ExportExcel(Guid id)
    {
        try
        {
            var report = await _service.GetByIdAsync(id);
            if (report is null) return NotFound();

            var result = await _service.ExecuteAsync(id);
            var data = _export.ExportToExcel(result, report.Name);

            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{report.Name}.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/export/pdf")]
    public async Task<ActionResult> ExportPdf(Guid id)
    {
        try
        {
            var report = await _service.GetByIdAsync(id);
            if (report is null) return NotFound();

            var result = await _service.ExecuteAsync(id);
            var data = _export.ExportToPdf(result, report.Name);

            return File(data, "application/pdf", $"{report.Name}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("export-adhoc/excel")]
    public async Task<ActionResult> ExportAdHocExcel(
        [FromQuery] string module,
        [FromQuery] string title,
        [FromBody] CreateCustomReportRequest request)
    {
        var result = await _service.ExecuteAdHocAsync(module, request);
        var data = _export.ExportToExcel(result, title);
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{title}.xlsx");
    }

    [HttpPost("export-adhoc/pdf")]
    public async Task<ActionResult> ExportAdHocPdf(
        [FromQuery] string module,
        [FromQuery] string title,
        [FromBody] CreateCustomReportRequest request)
    {
        var result = await _service.ExecuteAdHocAsync(module, request);
        var data = _export.ExportToPdf(result, title);
        return File(data, "application/pdf", $"{title}.pdf");
    }
}
