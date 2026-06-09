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
    private readonly FinancialReportService _reportService;
    private readonly EnhancedReportService _enhancedService;
    private readonly FinancialAssistantService _assistantService;
    public FinancialReportsController(
        FinancialReportService reportService,
        EnhancedReportService enhancedService,
        FinancialAssistantService assistantService)
    {
        _reportService = reportService;
        _enhancedService = enhancedService;
        _assistantService = assistantService;
    }

    [HttpGet("trial-balance/{periodId:guid}")]
    public async Task<IActionResult> TrialBalance(Guid periodId)
    {
        try
        {
            var result = await _reportService.GetTrialBalanceAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("income-statement/{periodId:guid}")]
    public async Task<IActionResult> IncomeStatement(Guid periodId)
    {
        try
        {
            var result = await _reportService.GetIncomeStatementAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("balance-sheet/{periodId:guid}")]
    public async Task<IActionResult> BalanceSheet(Guid periodId)
    {
        try
        {
            var result = await _reportService.GetBalanceSheetAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<IActionResult> GeneralLedger(Guid accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var result = await _reportService.GetGeneralLedgerAsync(accountId, fromDate, toDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("cost-center-expense/{costCenterId:guid}")]
    public async Task<IActionResult> CostCenterExpense(Guid costCenterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var result = await _reportService.GetCostCenterExpenseReportAsync(costCenterId, fromDate, toDate);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("budget-vs-actual")]
    public async Task<IActionResult> BudgetVsActual([FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var result = await _reportService.GetBudgetVsActualAsync(year, month);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("equity-changes/{periodId:guid}")]
    public async Task<IActionResult> EquityChanges(Guid periodId)
    {
        try
        {
            var result = await _enhancedService.GetEquityChangesAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("cash-flow/{periodId:guid}")]
    public async Task<IActionResult> CashFlow(Guid periodId)
    {
        try
        {
            var result = await _enhancedService.GetCashFlowStatementAsync(periodId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("comparative")]
    public async Task<IActionResult> Comparative([FromBody] ComparativeReportRequest request)
    {
        try
        {
            var result = await _enhancedService.GetComparativeReportAsync(request.ReportType, request.PeriodIds);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("assistant/ask")]
    public async Task<IActionResult> AskAssistant([FromBody] FinancialAssistantRequest request)
    {
        try
        {
            var result = await _assistantService.AskAsync(request.Query);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("assistant/feedback")]
    public async Task<IActionResult> AssistantFeedback([FromBody] FinancialAssistantFeedbackRequest request)
    {
        try
        {
            await _assistantService.SaveFeedbackAsync(request);
            return Ok();
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }
