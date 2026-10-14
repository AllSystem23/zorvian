using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/payroll/settlement")]
public sealed class SettlementController : ControllerBase
{
    private readonly ISettlementPdfService _pdfService;

    public SettlementController(ISettlementPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    [RequirePermission(Permissions.PayrollWrite)]
    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GenerateSettlementPdf([FromBody] GenerateSettlementPdfRequest request)
    {
        try
        {
            var pdfData = await _pdfService.GenerateSettlementPdfAsync(request.TerminationRecordId);
            return File(pdfData, "application/pdf", $"Liquidacion_{request.TerminationRecordId}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public record GenerateSettlementPdfRequest(Guid TerminationRecordId);
