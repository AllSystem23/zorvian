using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<ProductsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("Product", "Create")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        try
        {
            var product = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { error = "Product not found" });
        return Ok(product);
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ProductFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var items = await _service.GetLowStockAsync();
        return Ok(items);
    }

    [Audit("Product", "Update")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _service.UpdateAsync(id, request);
        if (product is null)
            return NotFound(new { error = "Product not found" });
        return Ok(product);
    }

    [Audit("Product", "Delete")]
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Product not found" });
        return NoContent();
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] InventoryMovementFilterRequest filter)
    {
        var result = await _service.GetMovementsAsync(filter);
        return Ok(result);
    }

    [Audit("Product", "GenerateKardexPdf")]
    [RequirePermission(Permissions.InventoryRead)]
    [HttpPost("{id:guid}/kardex/generate")]
    public async Task<IActionResult> GenerateKardexPdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("kardex", id);
            if (context is null)
                return NotFound(new { error = "Producto no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Kardex de Producto");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Kardex de Producto' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "kardex");

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
            _logger.LogError(ex, "Error generando kardex de producto {ProductId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
