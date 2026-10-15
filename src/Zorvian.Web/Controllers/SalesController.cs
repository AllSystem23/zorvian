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
[Route("zorvian/v1/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly SaleService _service;
    private readonly IDocumentTemplateRepository _documentTemplateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        SaleService service,
        IDocumentTemplateRepository documentTemplateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<SalesController> logger)
    {
        _service = service;
        _documentTemplateRepo = documentTemplateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("Sale", "Create")]
    [HttpPost("cash")]
    [RequirePermission(Permissions.SaleWrite)]
    public async Task<IActionResult> CreateCashSale([FromBody] CreateCashSaleRequest request)
    {
        var sale = await _service.CreateCashSaleAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [Audit("Sale", "Create")]
    [HttpPost("credit")]
    [RequirePermission(Permissions.SaleWrite)]
    public async Task<IActionResult> CreateCreditSale([FromBody] CreateCreditSaleRequest request)
    {
        var sale = await _service.CreateCreditSaleAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sale = await _service.GetByIdAsync(id);
        if (sale is null)
            return NotFound(new { error = "Sale not found" });
        return Ok(sale);
    }

    [HttpGet]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GetList([FromQuery] SaleFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Sale", "Cancel")]
    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(Permissions.SaleWrite)]
    public async Task<IActionResult> CancelSale(Guid id)
    {
        await _service.CancelSaleAsync(id);
        return Ok(new { message = "Sale cancelled successfully" });
    }

    [Audit("Sale", "GenerateInvoice")]
    [HttpPost("{id:guid}/invoice/generate")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GenerateInvoice(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("sale", id);
            if (context is null) return NotFound(new { error = "Venta no encontrada" });

            var template = await _documentTemplateRepo.GetByNameAsync("Factura");
            if (template is null) return NotFound(new { error = "Plantilla 'Factura' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "sale");

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
            _logger.LogError(ex, "Error generando factura para venta {SaleId}", id);
            return StatusCode(500, new { error = "Error interno al generar factura" });
        }
    }

    [Audit("Sale", "GenerateDeliveryGuide")]
    [HttpPost("{id:guid}/delivery-guide/generate")]
    [RequirePermission(Permissions.SaleRead)]
    public async Task<IActionResult> GenerateDeliveryGuide(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("sale", id);
            if (context is null) return NotFound(new { error = "Venta no encontrada" });

            var template = await _documentTemplateRepo.GetByNameAsync("Guía de Despacho");
            if (template is null) return NotFound(new { error = "Plantilla 'Guía de Despacho' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "sale");

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
            _logger.LogError(ex, "Error generando guía de despacho para venta {SaleId}", id);
            return StatusCode(500, new { error = "Error interno al generar guía de despacho" });
        }
    }
}
