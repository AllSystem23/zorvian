using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/accounting-periods")]
public sealed class AccountingPeriodsController : ControllerBase
{
    private readonly AccountingPeriodService _service;
    public AccountingPeriodsController(AccountingPeriodService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Open([FromBody] OpenPeriodRequest request)
    {
        try
        {
            var period = await _service.OpenAsync(request.Year, request.Month);
            return Ok(period);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            var period = await _service.CloseAsync(id);
            return Ok(period);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
