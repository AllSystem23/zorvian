using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/bi")]
public sealed class BiController : ControllerBase
{
    private readonly BiService _bi;

    public BiController(BiService bi) => _bi = bi;

    [HttpGet("executive-summary")]
    public async Task<IActionResult> GetExecutive()
    {
        var result = await _bi.GetExecutiveAsync();
        return Ok(result);
    }

    [HttpGet("sales-trend")]
    public async Task<IActionResult> GetSalesTrend([FromQuery] int months = 12)
    {
        var result = await _bi.GetSalesTrendAsync(months);
        return Ok(result);
    }

    [HttpGet("sales-by-category")]
    public async Task<IActionResult> GetSalesByCategory([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bi.GetSalesByCategoryAsync(from, to);
        return Ok(result);
    }

    [HttpGet("sales-by-seller")]
    public async Task<IActionResult> GetSalesBySeller([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bi.GetSalesBySellerAsync(from, to);
        return Ok(result);
    }

    [HttpGet("quote-conversion")]
    public async Task<IActionResult> GetQuoteConversion([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bi.GetQuoteConversionAsync(from, to);
        return Ok(result);
    }

    [HttpGet("ar-aging")]
    public async Task<IActionResult> GetArAging()
    {
        var result = await _bi.GetArAgingAsync();
        return Ok(result);
    }

    [HttpGet("ap-aging")]
    public async Task<IActionResult> GetApAging()
    {
        var result = await _bi.GetApAgingAsync();
        return Ok(result);
    }

    [HttpGet("financial-ratios")]
    public async Task<IActionResult> GetFinancialRatios([FromQuery] Guid? periodId)
    {
        try
        {
            var result = await _bi.GetFinancialRatiosAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("comparative-income")]
    public async Task<IActionResult> GetComparativeIncome([FromQuery] Guid? currentPeriodId, [FromQuery] Guid? previousPeriodId)
    {
        try
        {
            var result = await _bi.GetComparativeIncomeAsync(currentPeriodId, previousPeriodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("cash-flow")]
    public async Task<IActionResult> GetCashFlow([FromQuery] Guid? periodId)
    {
        try
        {
            var result = await _bi.GetCashFlowAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        var result = await _bi.GetInventorySummaryAsync();
        return Ok(result);
    }

    [HttpGet("payroll-summary")]
    public async Task<IActionResult> GetPayrollSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bi.GetPayrollSummaryAsync(from, to);
        return Ok(result);
    }

    [HttpGet("employee-turnover")]
    public async Task<IActionResult> GetEmployeeTurnover([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _bi.GetEmployeeTurnoverAsync(from, to);
        return Ok(result);
    }
}
