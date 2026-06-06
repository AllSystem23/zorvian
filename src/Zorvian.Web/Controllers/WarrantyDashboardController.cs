using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("api/v1/warranty-dashboard")]
[Authorize]
public sealed class WarrantyDashboardController : ControllerBase
{
    private readonly WarrantyDashboardService _service;

    public WarrantyDashboardController(WarrantyDashboardService service)
    {
        _service = service;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        return Ok(await _service.GetDashboardMetricsAsync());
    }
}
