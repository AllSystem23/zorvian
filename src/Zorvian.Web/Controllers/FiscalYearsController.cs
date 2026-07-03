using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Helpers;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/fiscal-years")]
public sealed class FiscalYearsController : ControllerBase
{
    private readonly FiscalYearService _service;
    public FiscalYearsController(FiscalYearService service) => _service = service;

    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>
    /// Returns the resolved fiscal year configuration for the current company,
    /// including the effective start month, label, and date range for a given year.
    /// Uses CompanySettings.FiscalYearStartMonth as primary source,
    /// falls back to country-based auto-detection.
    /// </summary>
    [HttpGet("config")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetConfig([FromQuery] int? year)
    {
        var result = await _service.GetFiscalYearConfigAsync(year ?? DateTime.UtcNow.Year);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Open([FromBody] OpenFiscalYearRequest request)
    {
        try
        {
            var fy = await _service.OpenAsync(request.Year, request.StartDate, request.EndDate);
            return Ok(fy);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            var fy = await _service.CloseAsync(id);
            return Ok(fy);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
