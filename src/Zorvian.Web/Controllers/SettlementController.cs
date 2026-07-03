using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Models;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/payroll/settlement")]
public sealed class SettlementController : ControllerBase
{
    private readonly ISettlementPdfService _pdfService;
    private readonly IPayrollLocalizationService _payrollService;

    public SettlementController(ISettlementPdfService pdfService, IPayrollLocalizationService payrollService)
    {
        _pdfService = pdfService;
        _payrollService = payrollService;
    }

    [RequirePermission(Permissions.PayrollWrite)]
    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GenerateSettlementPdf([FromBody] SettlementRequest request)
    {
        var context = new PayrollContext(1, false, new TerminationContext(request.TerminationType, request.HireDate, request.TerminationDate, request.Salary));
        var pdfData = await _pdfService.GenerateSettlementPdfAsync(request.CompanyId, request.EmployeeId, context);
        
        return File(pdfData, "application/pdf", $"Liquidacion_{request.EmployeeId}.pdf");
    }
}

public record SettlementRequest(Guid CompanyId, Guid EmployeeId, TerminationReason TerminationType, DateTime HireDate, DateTime TerminationDate, decimal Salary);
