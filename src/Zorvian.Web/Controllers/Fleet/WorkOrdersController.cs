using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/work-orders")]
public sealed class WorkOrdersController : ControllerBase
{
    private readonly WorkOrderService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<WorkOrdersController> _logger;

    public WorkOrdersController(
        WorkOrderService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<WorkOrdersController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? branchId)
    {
        var items = await _service.GetAllAsync(branchId);
        return Ok(new { items });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Work order not found" });
        return Ok(item);
    }

    [Audit("FleetWorkOrder", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Audit("FleetWorkOrder", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkOrderRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item is null) return NotFound(new { error = "Work order not found" });
        return Ok(item);
    }

    [Audit("FleetWorkOrder", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Work order not found" });
        return NoContent();
    }

    [Audit("FleetWorkOrder", "GeneratePdf")]
    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("workorder", id);
            if (context is null)
                return NotFound(new { error = "Orden de trabajo no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Orden de Trabajo");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Orden de Trabajo' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "workorder");

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
            _logger.LogError(ex, "Error generando orden de trabajo {WorkOrderId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }

    [Audit("FleetWorkOrder", "GeneratePdf")]
    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("{id:guid}/receipt/pdf/generate")]
    public async Task<IActionResult> GenerateReceptionReceiptPdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("workshopreceipt", id);
            if (context is null)
                return NotFound(new { error = "Orden de trabajo no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Acta de Recepción (Taller)");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Acta de Recepción (Taller)' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "workshopreceipt");

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
            _logger.LogError(ex, "Error generando acta de recepción de taller {WorkOrderId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
