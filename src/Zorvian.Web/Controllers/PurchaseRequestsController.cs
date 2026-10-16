using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.PurchaseRequest;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[Authorize]
[ApiController]
[Route("zorvian/v1/purchase-requests")]
public sealed class PurchaseRequestsController : ControllerBase
{
    private readonly PurchaseRequestService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<PurchaseRequestsController> _logger;

    public PurchaseRequestsController(
        PurchaseRequestService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<PurchaseRequestsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _service.GetFilteredAsync(
            search, status, priority, branchId, page, pageSize);
        return Ok(new PagedResult<PurchaseRequestResponse>(items, total, page, pageSize));
    }

    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequestRequest request)
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

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            return Ok(await _service.SubmitAsync(id));
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

    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id));
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

    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPurchaseRequestRequest request)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, request ?? new RejectPurchaseRequestRequest(null)));
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

    [RequirePermission(Permissions.PurchaseWrite)]
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

    [Audit("PurchaseRequest", "GeneratePdf")]
    [RequirePermission(Permissions.PurchaseRead)]
    [HttpPost("{id}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("purchaserequest", id);
            if (context is null)
                return NotFound(new { detail = "Solicitud de cotización no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Solicitud de Cotización");
            if (template is null)
                return NotFound(new { detail = "Plantilla 'Solicitud de Cotización' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "purchaserequest");

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
            _logger.LogError(ex, "Error generando PDF de solicitud de cotización {PurchaseRequestId}", id);
            return StatusCode(500, new { detail = "Error interno al generar el documento" });
        }
    }
}
