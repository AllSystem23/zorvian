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
[Route("zorvian/v1/fleet/deliveries")]
public sealed class DeliveriesController : ControllerBase
{
    private readonly DeliveryService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(
        DeliveryService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<DeliveriesController> logger)
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
        var deliveries = await _service.GetAllAsync(branchId);
        return Ok(new { items = deliveries });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var delivery = await _service.GetByIdAsync(id);
        if (delivery is null) return NotFound(new { error = "Delivery not found" });
        return Ok(delivery);
    }

    [Audit("FleetDelivery", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryRequest request)
    {
        var delivery = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);
    }

    [Audit("FleetDelivery", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDeliveryRequest request)
    {
        var delivery = await _service.UpdateAsync(id, request);
        if (delivery is null) return NotFound(new { error = "Delivery not found" });
        return Ok(delivery);
    }

    [Audit("FleetDelivery", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Delivery not found" });
        return NoContent();
    }

    [Audit("FleetDelivery", "GeneratePdf")]
    [RequirePermission(Permissions.FleetRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("delivery", id);
            if (context is null)
                return NotFound(new { error = "Entrega no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Hoja de Ruta / Entrega");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Hoja de Ruta / Entrega' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "delivery");

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
            _logger.LogError(ex, "Error generando hoja de ruta {DeliveryId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
