using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

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

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("trial-balance/{periodId:guid}")]
    public async Task<IActionResult> TrialBalance(Guid periodId)
    {
        var result = await _reportService.GetTrialBalanceAsync(periodId);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("income-statement/{periodId:guid}")]
    public async Task<IActionResult> IncomeStatement(Guid periodId)
    {
        var result = await _reportService.GetIncomeStatementAsync(periodId);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("balance-sheet/{periodId:guid}")]
    public async Task<IActionResult> BalanceSheet(Guid periodId)
    {
        var result = await _reportService.GetBalanceSheetAsync(periodId);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<IActionResult> GeneralLedger(Guid accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _reportService.GetGeneralLedgerAsync(accountId, fromDate, toDate);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("cost-center-expense/{costCenterId:guid}")]
    public async Task<IActionResult> CostCenterExpense(Guid costCenterId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _reportService.GetCostCenterExpenseReportAsync(costCenterId, fromDate, toDate);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("budget-vs-actual")]
    public async Task<IActionResult> BudgetVsActual([FromQuery] int year, [FromQuery] int month)
    {
        var result = await _reportService.GetBudgetVsActualAsync(year, month);
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("equity-changes/{periodId:guid}")]
    public async Task<IActionResult> EquityChanges(Guid periodId)
    {
        var result = await _enhancedService.GetEquityChangesAsync(periodId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpGet("cash-flow/{periodId:guid}")]
    public async Task<IActionResult> CashFlow(Guid periodId)
    {
        var result = await _enhancedService.GetCashFlowStatementAsync(periodId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpPost("comparative")]
    public async Task<IActionResult> Comparative([FromBody] ComparativeReportRequest request)
    {
        var result = await _enhancedService.GetComparativeReportAsync(request.ReportType, request.PeriodIds);
        if (result is null) return BadRequest(new { error = "At least two periods are required for comparison" });
        return Ok(result);
    }

    [RequirePermission(Permissions.ReportRead)]
    [HttpPost("assistant/ask")]
    public async Task<IActionResult> AskAssistant([FromBody] FinancialAssistantRequest request)
    {
        var result = await _assistantService.AskAsync(request.Query);
        return Ok(result);
    }
    [RequirePermission(Permissions.ReportRead)]
    [HttpPost("assistant/feedback")]
    public async Task<IActionResult> AssistantFeedback([FromBody] FinancialAssistantFeedbackRequest request)
    {
        await _assistantService.SaveFeedbackAsync(request);
        return Ok();
    }
}
