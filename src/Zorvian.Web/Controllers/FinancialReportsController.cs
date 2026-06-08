using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/financial-reports")]
public sealed class FinancialReportsController : ControllerBase
{
    private readonly FinancialReportService _service;
    public FinancialReportsController(FinancialReportService service) => _service = service;

    [HttpGet("trial-balance/{periodId:guid}")]
    public async Task<IActionResult> TrialBalance(Guid periodId)
    {
        try
        {
            var result = await _service.GetTrialBalanceAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("income-statement/{periodId:guid}")]
    public async Task<IActionResult> IncomeStatement(Guid periodId)
    {
        try
        {
            var result = await _service.GetIncomeStatementAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("balance-sheet/{periodId:guid}")]
    public async Task<IActionResult> BalanceSheet(Guid periodId)
    {
        try
        {
            var result = await _service.GetBalanceSheetAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<IActionResult> GeneralLedger(Guid accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var result = await _service.GetGeneralLedgerAsync(accountId, fromDate, toDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("cost-center-expense/{costCenterId:guid}")]
    public async Task<IActionResult> CostCenterExpense(Guid costCenterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var result = await _service.GetCostCenterExpenseReportAsync(costCenterId, fromDate, toDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("budget-vs-actual")]
    public async Task<IActionResult> BudgetVsActual([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var result = await _service.GetBudgetVsActualAsync(year, month);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
