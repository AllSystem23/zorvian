using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/reports")]
public sealed class FleetReportsController : ControllerBase
{
    private readonly FleetReportService _reportService;
    private readonly IReportExportService _exportService;
    private readonly Zorvian.Infrastructure.Services.FleetPdfChartService _pdfChart;

    public FleetReportsController(FleetReportService reportService, IReportExportService exportService, Zorvian.Infrastructure.Services.FleetPdfChartService pdfChart)
    {
        _reportService = reportService;
        _exportService = exportService;
        _pdfChart = pdfChart;
    }

    // ── Operational Reports ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("vehicle-usage")]
    public async Task<IActionResult> GetVehicleUsage([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetVehicleUsageAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveryReport([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetDeliveryReportAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("routes")]
    public async Task<IActionResult> GetRouteReport([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetRouteReportAsync(request);
        return Ok(result);
    }

    // ── Financial Reports ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("cost-summary")]
    public async Task<IActionResult> GetCostSummary([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetCostSummaryAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("cost-by-vehicle")]
    public async Task<IActionResult> GetCostByVehicle([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetCostByVehicleAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("cost-trend")]
    public async Task<IActionResult> GetCostTrend([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetCostTrendAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("profitability")]
    public async Task<IActionResult> GetProfitability([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetProfitabilityAsync(request);
        return Ok(result);
    }

    // ── Managerial Reports ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("kpis")]
    public async Task<IActionResult> GetFleetKpis()
    {
        var result = await _reportService.GetFleetKpisAsync();
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("driver-scorecard")]
    public async Task<IActionResult> GetDriverScorecard([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetDriverScorecardAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("vehicle-scorecard")]
    public async Task<IActionResult> GetVehicleScorecard([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetVehicleScorecardAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("fuel-trend")]
    public async Task<IActionResult> GetFuelTrend([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetFuelTrendAsync(request);
        return Ok(result);
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("expense-by-account")]
    public async Task<IActionResult> GetExpenseByAccount([FromQuery] FleetReportRequest request)
    {
        var result = await _reportService.GetExpenseByAccountAsync(request);
        return Ok(result);
    }

    // ── PDF Charts ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("expense-by-account/pdf")]
    public async Task<IActionResult> GetExpenseByAccountPdf(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var request = new FleetReportRequest(startDate, endDate, null, null, null, null);
        var report = await _reportService.GetExpenseByAccountAsync(request);
        var pdf = _pdfChart.GenerateExpenseByAccountPdf(report, startDate, endDate);
        return File(pdf, "application/pdf", $"Gastos_Por_Cuenta_{DateTime.Now:yyyyMMdd}.pdf");
    }

    // ── Export ──

    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] FleetExportRequest request)
    {
        var reportResult = await _reportService.GetReportAsResultAsync(request);
        var title = $"Reporte Flota — {request.ReportType}";

        if (string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var excel = _exportService.ExportToExcel(reportResult, title);
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{title}.xlsx");
        }

        var pdf = _exportService.ExportToPdf(reportResult, title);
        return File(pdf, "application/pdf", $"{title}.pdf");
    }
}
