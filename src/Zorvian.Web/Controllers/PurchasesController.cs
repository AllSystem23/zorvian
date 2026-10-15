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
[Route("zorvian/v1/purchases")]
public sealed class PurchasesController : ControllerBase
{
    private readonly PurchaseService _service;
    private readonly IPurchaseIntelligenceService _aiService;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<PurchasesController> _logger;

    public PurchasesController(
        PurchaseService service,
        IPurchaseIntelligenceService aiService,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<PurchasesController> logger)
    {
        _service = service;
        _aiService = aiService;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeInvoice(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { error = "No file uploaded" });

        using var stream = file.OpenReadStream();
        var result = await _aiService.AnalyzeInvoiceAsync(stream);
        return Ok(result);
    }

    [Audit("Purchase", "Create")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequest request)
    {
        try
        {
            var purchase = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var purchase = await _service.GetByIdAsync(id);
        if (purchase is null)
            return NotFound(new { error = "Purchase not found" });
        return Ok(purchase);
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] PurchaseFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Purchase", "Update")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseRequest request)
    {
        try
        {
            var purchase = await _service.UpdateAsync(id, request);
            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("Purchase", "Complete")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _service.CompleteAsync(id);
        return NoContent();
    }

    [Audit("Purchase", "Cancel")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var purchase = await _service.CancelAsync(id);
            return Ok(purchase);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("aging")]
    public async Task<IActionResult> GetAging()
    {
        var result = await _service.GetAgingAsync();
        return Ok(result);
    }

    [Audit("Purchase", "GeneratePdf")]
    [RequirePermission(Permissions.PurchaseRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("purchase", id);
            if (context is null)
                return NotFound(new { error = "Compra no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Acta de Recepción de Compras");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Acta de Recepción de Compras' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "purchase");

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
            _logger.LogError(ex, "Error generando acta de recepción de compra {PurchaseId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
