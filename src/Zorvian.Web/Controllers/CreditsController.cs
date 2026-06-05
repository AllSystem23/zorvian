using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/credits")]
public sealed class CreditsController : ControllerBase
{
    private readonly CreditService _service;

    public CreditsController(CreditService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var credit = await _service.GetByIdAsync(id);
        if (credit is null)
            return NotFound(new { error = "Credit not found" });
        return Ok(credit);
    }

    [HttpGet]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetList([FromQuery] CreditFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("CreditPayment", "Create")]
    [HttpPost("{id:guid}/payments")]
    [RequirePermission(Permissions.CreditWrite)]
    public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] CreateCreditPaymentRequest request)
    {
        try
        {
            var payment = await _service.RegisterPaymentAsync(request with { CreditId = id });
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/payments")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetPayments(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPaymentsAsync(id, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:guid}/late-fees/calculate")]
    [RequirePermission(Permissions.CreditWrite)]
    public async Task<IActionResult> CalculateLateFees(Guid id, [FromBody] CalculateLateFeeRequest? request)
    {
        try
        {
            var result = await _service.CalculateLateFeesAsync(id, request?.DailyInterestRate);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/late-fees")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetLateFees(Guid id)
    {
        var result = await _service.GetLateFeesAsync(id);
        return Ok(result);
    }

    [HttpGet("{id:guid}/overdue-installments")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetOverdueInstallments(Guid id)
    {
        var result = await _service.GetOverdueInstallmentsAsync(id);
        return Ok(result);
    }

    [Audit("CollectionAction", "Create")]
    [HttpPost("{id:guid}/collection-actions")]
    [RequirePermission(Permissions.CreditWrite)]
    public async Task<IActionResult> AddCollectionAction(Guid id, [FromBody] CreateCollectionActionRequest request)
    {
        try
        {
            var result = await _service.AddCollectionActionAsync(request with { CreditId = id });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/collection-actions")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetCollectionActions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetCollectionActionsAsync(id, page, pageSize);
        return Ok(result);
    }
}
