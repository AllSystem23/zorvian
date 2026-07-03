using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/accounting-periods")]
public sealed class AccountingPeriodsController : ControllerBase
{
    private readonly AccountingPeriodService _service;
    public AccountingPeriodsController(AccountingPeriodService service) => _service = service;

    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Open([FromBody] OpenPeriodRequest request)
    {
        try
        {
            var period = await _service.OpenAsync(request.Year, request.Month, request.FiscalYearId);
            return Ok(period);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Close(Guid id, [FromBody] ClosePeriodRequest request)
    {
        var period = await _service.CloseAsync(id, request?.Notes);
        return Ok(period);
    }

    [HttpPost("{id:guid}/reopen")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Reopen(Guid id, [FromBody] ReopenPeriodRequest request)
    {
        try
        {
            var period = await _service.ReopenAsync(id, request.Reason);
            return Ok(period);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/validate-close")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> ValidateClose(Guid id)
    {
        var validation = await _service.ValidateCloseAsync(id);
        return Ok(validation);
    }
}
