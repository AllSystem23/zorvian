using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranty-profitability")]
public sealed class WarrantyProfitabilityController : ControllerBase
{
    private readonly WarrantyProfitabilityReportService _service;

    public WarrantyProfitabilityController(WarrantyProfitabilityReportService service) => _service = service;

    [HttpGet("cost-summary/{warrantyId:guid}")]
    [RequirePermission(Permissions.WarrantyRead)]
    public async Task<IActionResult> GetCostByWarranty(Guid warrantyId)
    {
        var result = await _service.GetCostByWarrantyAsync(warrantyId);
        return Ok(result);
    }

    [HttpGet("report")]
    [RequirePermission(Permissions.WarrantyViewProfitability)]
    public async Task<IActionResult> GetProfitabilityReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-12);
        var toDate = to ?? DateTime.UtcNow;
        var result = await _service.GetProfitabilityReportAsync(fromDate, toDate);
        return Ok(result);
    }
}
