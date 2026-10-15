using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/supplier-payments")]
public sealed class SupplierPaymentsController : ControllerBase
{
    private readonly SupplierPaymentService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<SupplierPaymentsController> _logger;

    public SupplierPaymentsController(
        SupplierPaymentService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<SupplierPaymentsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("SupplierPayment", "Create")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost]
    public async Task<IActionResult> RegisterPayment([FromBody] CreateSupplierPaymentRequest request)
    {
        try
        {
            var payment = await _service.RegisterPaymentAsync(request);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _service.GetAllAsync();
        return Ok(payments);
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("by-purchase/{purchaseId:guid}")]
    public async Task<IActionResult> GetByPurchaseId(Guid purchaseId)
    {
        var payments = await _service.GetByPurchaseIdAsync(purchaseId);
        return Ok(payments);
    }

    [Audit("SupplierPayment", "GeneratePdf")]
    [RequirePermission(Permissions.PurchaseRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("supplierpayment", id);
            if (context is null)
                return NotFound(new { error = "Pago a proveedor no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Recibo de Pago (Proveedor)");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Recibo de Pago (Proveedor)' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "supplierpayment");

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
            _logger.LogError(ex, "Error generando recibo de pago a proveedor {PaymentId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
