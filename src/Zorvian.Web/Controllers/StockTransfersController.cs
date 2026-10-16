using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.StockTransfer;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[Authorize]
[ApiController]
[Route("zorvian/v1/stock-transfers")]
public sealed class StockTransfersController : ControllerBase
{
    private readonly StockTransferService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<StockTransfersController> _logger;

    public StockTransfersController(
        StockTransferService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<StockTransfersController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _service.GetFilteredAsync(
            search, status, branchId, warehouseId, page, pageSize);
        return Ok(new PagedResult<StockTransferResponse>(items, total, page, pageSize));
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> Dispatch(Guid id)
    {
        try
        {
            return Ok(await _service.DispatchAsync(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Recepción total (body vacío/omitido o sin líneas) o parcial
    /// (líneas con DetailId + Quantity a recibir en esta tanda).
    /// </summary>
    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(
        Guid id,
        [FromBody] ReceiveStockTransferRequest? request = null)
    {
        try
        {
            return Ok(await _service.ReceiveAsync(id, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.InventoryWrite)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            return Ok(await _service.CancelAsync(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [Audit("StockTransfer", "GeneratePdf")]
    [RequirePermission(Permissions.InventoryRead)]
    [HttpPost("{id}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("stocktransfer", id);
            if (context is null)
                return NotFound(new { detail = "Transferencia no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Transferencia entre Bodegas");
            if (template is null)
                return NotFound(new { detail = "Plantilla 'Transferencia entre Bodegas' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "stocktransfer");

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
            return BadRequest(new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando PDF de transferencia {StockTransferId}", id);
            return StatusCode(500, new { detail = "Error interno al generar el documento" });
        }
    }
}
