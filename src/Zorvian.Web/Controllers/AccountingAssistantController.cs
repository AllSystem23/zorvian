using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.ML;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Asistente contable (anomalías de asientos). Superficie de BI: la consume el
/// dashboard financiero, que ya exige `kpi.read`, así que el endpoint usa el
/// mismo permiso.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/accounting-assistant")]
public sealed class AccountingAssistantController : ControllerBase
{
    private readonly AccountingAssistantService _service;
    private readonly ExpenseClassificationService _classifier;
    private readonly ITenantContext _tenant;

    public AccountingAssistantController(
        AccountingAssistantService service,
        ExpenseClassificationService classifier,
        ITenantContext tenant)
    {
        _service = service;
        _classifier = classifier;
        _tenant = tenant;
    }

    [HttpGet("anomalies")]
    [RequirePermission(Permissions.KpiRead)]
    public async Task<IActionResult> GetAnomalies([FromQuery] int daysBack = 30)
    {
        var result = await _service.DetectAnomaliesAsync(daysBack);
        return Ok(result);
    }
}
