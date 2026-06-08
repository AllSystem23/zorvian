using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.ML;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Controllers;

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
    public async Task<IActionResult> GetAnomalies([FromQuery] int daysBack = 30)
    {
        var result = await _service.DetectAnomaliesAsync(daysBack);
        return Ok(result);
    }
}
