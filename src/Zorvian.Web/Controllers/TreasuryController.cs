using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Treasury;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/treasury")]
public sealed class TreasuryController : ControllerBase
{
    private readonly ITreasuryService _treasury;
    private readonly IAutoAccountingService _accounting;

    public TreasuryController(ITreasuryService treasury, IAutoAccountingService accounting)
    {
        _treasury = treasury;
        _accounting = accounting;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : Guid.NewGuid();

    [HttpPost("checks/issue")]
    public async Task<IActionResult> IssueCheck([FromBody] Check check)
    {
        var result = await _treasury.IssueCheckAsync(check, CurrentUserId);
        return Ok(result);
    }

    [HttpPut("checks/{id:guid}/status")]
    public async Task<IActionResult> UpdateCheckStatus(Guid id, [FromBody] CheckStatus newStatus, [FromQuery] string? remarks)
    {
        await _treasury.UpdateCheckStatusAsync(id, newStatus, CurrentUserId, remarks);
        return NoContent();
    }

    [HttpGet("checks/print-template/{bankId:guid}")]
    public async Task<IActionResult> GetPrintTemplate(Guid bankId)
    {
        var template = await _treasury.GetPrintTemplateAsync(bankId);
        return template is not null ? Ok(template) : NotFound();
    }

    [HttpPost("checks/accounting-entry")]
    public async Task<IActionResult> GenerateCheckEntry([FromBody] GenerateCheckEntryRequest request)
    {
        var entryId = await _accounting.GenerateCheckEntryAsync(
            request.CheckId, request.Amount, request.CheckType,
            request.BankAccountId, request.PayeeId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de cheque generado"));
    }

    [HttpPost("bank-deposits/accounting-entry")]
    public async Task<IActionResult> GenerateBankDepositEntry([FromBody] GenerateBankDepositRequest request)
    {
        var entryId = await _accounting.GenerateBankDepositEntryAsync(
            request.BankMovementId, request.Amount, request.BankAccountId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de depósito generado"));
    }

    [HttpPost("bank-transfers/accounting-entry")]
    public async Task<IActionResult> GenerateBankTransferEntry([FromBody] GenerateBankTransferRequest request)
    {
        var entryId = await _accounting.GenerateBankTransferEntryAsync(
            request.BankMovementId, request.Amount, request.FromAccountId, request.ToAccountId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de transferencia generado"));
    }

    [HttpPost("bank-commissions/accounting-entry")]
    public async Task<IActionResult> GenerateBankCommissionEntry([FromBody] GenerateBankCommissionRequest request)
    {
        var entryId = await _accounting.GenerateBankCommissionEntryAsync(
            request.BankMovementId, request.Commission, request.BankAccountId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de comisión generado"));
    }

    [HttpPost("collections/accounting-entry")]
    public async Task<IActionResult> GenerateCollectionEntry([FromBody] GenerateCollectionRequest request)
    {
        var entryId = await _accounting.GenerateCollectionEntryAsync(
            request.PaymentId, request.Amount, request.Interest, request.LateFee, request.InvoiceId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de cobranza generado"));
    }

    [HttpPost("advances-to-suppliers/accounting-entry")]
    public async Task<IActionResult> GenerateAdvanceEntry([FromBody] GenerateAdvanceToSupplierRequest request)
    {
        var entryId = await _accounting.GenerateAdvanceToSupplierEntryAsync(
            request.AdvanceId, request.Amount, request.SupplierId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de anticipo generado"));
    }

    [HttpPost("supplier-advance-applications/accounting-entry")]
    public async Task<IActionResult> GenerateAdvanceApplicationEntry([FromBody] GenerateSupplierAdvanceApplicationRequest request)
    {
        var entryId = await _accounting.GenerateSupplierAdvanceApplicationEntryAsync(
            request.ApplicationId, request.Amount, request.AdvanceId, request.PurchaseId, request.CostCenterId);
        return Ok(new TreasuryEntryResponse(entryId, "Asiento contable de aplicación de anticipo generado"));
    }
}
