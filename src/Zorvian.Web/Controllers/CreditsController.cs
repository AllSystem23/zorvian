using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
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
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<CreditsController> _logger;

    public CreditsController(
        CreditService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<CreditsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("Credit", "Read")]
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var credit = await _service.GetByIdAsync(id);
        if (credit is null)
            return NotFound(new { error = "Credit not found" });
        return Ok(credit);
    }

    [Audit("Credit", "ReadList")]
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
        var payment = await _service.RegisterPaymentAsync(request with { CreditId = id });
        return Ok(payment);
    }

    [Audit("Credit", "ReadPayments")]
    [HttpGet("{id:guid}/payments")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetPayments(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPaymentsAsync(id, page, pageSize);
        return Ok(result);
    }

    [Audit("CreditPayment", "GeneratePdf")]
    [HttpPost("payments/{paymentId:guid}/pdf/generate")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GeneratePaymentReceiptPdf(Guid paymentId)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("creditpayment", paymentId);
            if (context is null)
                return NotFound(new { error = "Pago no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Recibo de Pago (Cliente)");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Recibo de Pago (Cliente)' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                paymentId,
                context.Data,
                "creditpayment");

            return Ok(new
            {
                generatedDocumentId = generated.Id,
                name = generated.Name,
                status = generated.Status,
                createdAt = generated.CreatedAt,
                downloadUrl = $"/zorvian/v1/documents/{generated.Id}/pdf"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando recibo de pago {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }

    [HttpPost("{id:guid}/late-fees/calculate")]
    [RequirePermission(Permissions.CreditWrite)]
    public async Task<IActionResult> CalculateLateFees(Guid id, [FromBody] CalculateLateFeeRequest? request)
    {
        var result = await _service.CalculateLateFeesAsync(id, request?.DailyInterestRate);
        return Ok(result);
    }

    [Audit("Credit", "ReadLateFees")]
    [HttpGet("{id:guid}/late-fees")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetLateFees(Guid id)
    {
        var result = await _service.GetLateFeesAsync(id);
        return Ok(result);
    }

    [Audit("Credit", "ReadOverdue")]
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

    [Audit("Credit", "ReadCollectionActions")]
    [HttpGet("{id:guid}/collection-actions")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetCollectionActions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetCollectionActionsAsync(id, page, pageSize);
        return Ok(result);
    }

    [Audit("Credit", "Refinancing")]
    [HttpPost("{id:guid}/refinancing")]
    [RequirePermission(Permissions.CreditWrite)]
    public async Task<IActionResult> CreateRefinancing(Guid id, [FromBody] CreateRefinancingRequest request)
    {
        try
        {
            var result = await _service.CreateRefinancingAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("Credit", "ReadRefinancings")]
    [HttpGet("{id:guid}/refinancings")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetRefinancings(Guid id)
    {
        var result = await _service.GetRefinancingsAsync(id);
        return Ok(result);
    }

    [Audit("Credit", "OverdueDashboard")]
    [HttpGet("overdue-dashboard")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetOverdueDashboard()
    {
        var result = await _service.GetOverdueDashboardAsync();
        return Ok(result);
    }

    [HttpGet("overdue-dashboard/late-fees")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetAllLateFees()
    {
        var result = await _service.GetAllLateFeesAsync();
        return Ok(result);
    }

    [HttpGet("overdue-dashboard/refinancings")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetAllRefinancings()
    {
        var result = await _service.GetAllRefinancingsAsync();
        return Ok(result);
    }

    [HttpGet("overdue-dashboard/collection-actions")]
    [RequirePermission(Permissions.CreditRead)]
    public async Task<IActionResult> GetAllCollectionActions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllCollectionActionsAsync(page, pageSize);
        return Ok(result);
    }
}
