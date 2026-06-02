using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Report;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de reportes. Genera archivos Excel con reportes de vacaciones, permisos, asistencia y saldos.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Genera y descarga un reporte en Excel según el tipo solicitado (vacation, permission, attendance, balance).
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReportRequest request)
    {
        byte[] data;
        string fileName;
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        switch (request.ReportType)
        {
            case "vacation":
                data = await _service.GenerateVacationReportAsync(request.Year ?? DateTime.UtcNow.Year);
                fileName = $"reporte_vacaciones_{request.Year ?? DateTime.UtcNow.Year}.xlsx";
                break;
            case "permission":
                data = await _service.GeneratePermissionReportAsync(request.Year ?? DateTime.UtcNow.Year);
                fileName = $"reporte_permisos_{request.Year ?? DateTime.UtcNow.Year}.xlsx";
                break;
            case "attendance":
                var year = request.Year ?? DateTime.UtcNow.Year;
                var month = request.Month ?? DateTime.UtcNow.Month;
                data = await _service.GenerateAttendanceReportAsync(year, month);
                fileName = $"reporte_asistencia_{year}_{month:D2}.xlsx";
                break;
            case "balance":
                data = await _service.GenerateBalanceReportAsync();
                fileName = $"reporte_saldos_vacaciones.xlsx";
                break;
            default:
                return BadRequest(new { error = "Tipo de reporte inválido. Use: vacation, permission, attendance, balance" });
        }

        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        return File(data, contentType, fileName);
    }
}
