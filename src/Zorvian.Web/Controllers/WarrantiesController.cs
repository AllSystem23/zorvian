using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Enums;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranties")]
public sealed class WarrantiesController : ControllerBase
{
    private readonly WarrantyService _service;
    private readonly WarrantyTimelineService _timelineService;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<WarrantiesController> _logger;

    public WarrantiesController(
        WarrantyService service,
        WarrantyTimelineService timelineService,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<WarrantiesController> logger)
    {
        _service = service;
        _timelineService = timelineService;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("Warranty", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
    {
        var warranty = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = warranty.Id }, warranty);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var warranty = await _service.GetByIdAsync(id);
        if (warranty is null)
            return NotFound(new { error = "Warranty not found" });
        return Ok(warranty);
    }

    [Audit("Warranty", "Update")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarrantyRequest request)
    {
        var warranty = await _service.UpdateAsync(id, request);
        return Ok(warranty);
    }

    [Audit("Warranty", "Delete")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [Audit("Warranty", "UpdateStatus")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWarrantyStatusRequest request)
    {
        if (!Enum.TryParse<WarrantyStatus>(request.Status, true, out var status))
            return BadRequest(new { error = "Invalid status value" });
        var warranty = await _service.UpdateStatusAsync(id, status);
        return Ok(warranty);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] WarrantyFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("WarrantyClaim", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("{id:guid}/claims")]
    public async Task<IActionResult> AddClaim(Guid id, [FromBody] CreateWarrantyClaimRequest request)
    {
        var claim = await _service.AddClaimAsync(request with { WarrantyId = id });
        return Ok(claim);
    }

    [Audit("WarrantyClaim", "AssignWorkshop")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("claims/{claimId:guid}/assign-workshop")]
    public async Task<IActionResult> AssignWorkshop(Guid claimId, [FromBody] AssignWorkshopRequest request)
    {
        var claim = await _service.AssignWorkshopAsync(claimId, request);
        return Ok(claim);
    }

    [Audit("WarrantyClaim", "ReferToProvider")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("claims/{claimId:guid}/refer-to-provider")]
    public async Task<IActionResult> ReferToProvider(Guid claimId, [FromBody] ReferToProviderRequest request)
    {
        var claim = await _service.ReferToProviderAsync(claimId, request);
        return Ok(claim);
    }

    [Audit("WarrantyClaim", "ProcessReplacement")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost("claims/{claimId:guid}/process-replacement")]
    public async Task<IActionResult> ProcessReplacement(Guid claimId, [FromBody] ProcessReplacementRequest request)
    {
        var claim = await _service.ProcessManufacturerReplacementAsync(claimId, request);
        return Ok(claim);
    }

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id)
    {
        return Ok(await _timelineService.GetTimelineAsync(id));
    }

    [Audit("Warranty", "GeneratePdf")]
    [RequirePermission(Permissions.WarrantyRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("warranty", id);
            if (context is null)
                return NotFound(new { error = "Garantía no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Certificado de Garantía");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Certificado de Garantía' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "warranty");

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
            _logger.LogError(ex, "Error generando certificado de garantía {WarrantyId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
