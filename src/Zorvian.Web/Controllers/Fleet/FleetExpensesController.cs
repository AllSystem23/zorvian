using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/expenses")]
public sealed class FleetExpensesController : ControllerBase
{
    private readonly FleetExpenseService _service;
    private readonly IExpenseClassificationService _classifier;
    private readonly IReportExportService _exportService;

    public FleetExpensesController(FleetExpenseService service, IExpenseClassificationService classifier, IReportExportService exportService)
    {
        _service = service;
        _classifier = classifier;
        _exportService = exportService;
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(new { items });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Expense not found" });
        return Ok(item);
    }

    [Audit("FleetExpense", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFleetExpenseRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("FleetExpense", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFleetExpenseRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Expense not found" });
        return Ok(item);
    }

    [Audit("FleetExpense", "Approve")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromQuery] Guid? accountId)
    {
        var item = await _service.ApproveAsync(id, accountId);
        if (item is null) return NotFound(new { error = "Expense not found" });
        return Ok(item);
    }

    [Audit("FleetExpense", "BatchApprove")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("approve-batch")]
    public async Task<IActionResult> ApproveBatch([FromBody] ApproveBatchRequest request)
    {
        var result = await _service.ApproveBatchAsync(request.Ids);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("classify")]
    public IActionResult Classify([FromBody] ClassifyExpenseRequest request)
    {
        var result = _classifier.Predict(request.Description, request.Amount);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Expense not found" });
        return NoContent();
    }

    // ── Export ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string format = "xlsx",
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? vehicleId = null)
    {
        var result = await _service.GetExportResultAsync(status, startDate, endDate, vehicleId);
        var filters = new List<string>();
        if (!string.IsNullOrEmpty(status)) filters.Add(status);
        if (startDate.HasValue) filters.Add($"desde_{startDate:yyyyMMdd}");
        if (endDate.HasValue) filters.Add($"hasta_{endDate:yyyyMMdd}");
        var title = filters.Count > 0 ? $"Gastos de Flota ({string.Join(", ", filters)})" : "Gastos de Flota";

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdf = _exportService.ExportToPdf(result, title);
            return File(pdf, "application/pdf", $"{title}.pdf");
        }

        var excel = _exportService.ExportToExcel(result, title);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{title}.xlsx");
    }
}

public sealed record ClassifyExpenseRequest(string Description, decimal Amount);
public sealed record ApproveBatchRequest(List<Guid> Ids);
