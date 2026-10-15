using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/fixed-assets")]
public sealed class FixedAssetsController : ControllerBase
{
    private readonly FixedAssetService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<FixedAssetsController> _logger;

    public FixedAssetsController(
        FixedAssetService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<FixedAssetsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("FixedAsset", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetRequest request)
    {
        try
        {
            var asset = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.FixedAssetRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var asset = await _service.GetByIdAsync(id);
        if (asset is null)
            return NotFound(new { error = "Asset not found" });
        return Ok(asset);
    }

    [Audit("FixedAsset", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFixedAssetRequest request)
    {
        try
        {
            var asset = await _service.UpdateAsync(id, request);
            return Ok(asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [RequirePermission(Permissions.FixedAssetRead)]
    public async Task<IActionResult> GetFiltered([FromQuery] FixedAssetFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("FixedAsset", "Depreciate")]
    [HttpPost("{id:guid}/depreciate")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> RunDepreciation(Guid id, [FromBody] RunDepreciationRequest request)
    {
        try
        {
            var entry = await _service.RunDepreciationAsync(id, request);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("FixedAsset", "BulkDepreciate")]
    [HttpPost("depreciate-bulk")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> RunBulkDepreciation([FromBody] RunDepreciationRequest request)
    {
        var count = await _service.RunBulkDepreciationAsync(request.PeriodDate);
        return Ok(new { depreciated = count });
    }

    [Audit("FixedAsset", "Revalue")]
    [HttpPost("{id:guid}/revalue")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> Revalue(Guid id, [FromBody] RevalueAssetRequest request)
    {
        var revaluation = await _service.RevalueAsync(id, request);
        return Ok(revaluation);
    }

    [Audit("FixedAsset", "Dispose")]
    [HttpPost("{id:guid}/dispose")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> Dispose(Guid id, [FromBody] DisposeAssetRequest request)
    {
        try
        {
            var disposal = await _service.DisposeAsync(id, request);
            return Ok(disposal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("FixedAsset", "AddMaintenance")]
    [HttpPost("{id:guid}/maintenance")]
    [RequirePermission(Permissions.FixedAssetWrite)]
    public async Task<IActionResult> AddMaintenance(Guid id, [FromBody] AddMaintenanceRequest request)
    {
        var maintenance = await _service.AddMaintenanceAsync(id, request);
        return CreatedAtAction(null, maintenance);
    }

    [HttpGet("{id:guid}/depreciation-schedule")]
    [RequirePermission(Permissions.FixedAssetRead)]
    public async Task<IActionResult> GetDepreciationSchedule(Guid id)
    {
        var schedule = await _service.GetDepreciationScheduleAsync(id);
        return Ok(schedule);
    }

    [HttpGet("reports/summary")]
    [RequirePermission(Permissions.FixedAssetRead)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _service.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("maintenance/upcoming")]
    [RequirePermission(Permissions.FixedAssetRead)]
    public async Task<IActionResult> GetUpcomingMaintenance([FromQuery] int days = 30)
    {
        var items = await _service.GetUpcomingMaintenanceAsync(days);
        return Ok(items);
    }

    [Audit("FixedAsset", "GeneratePdf")]
    [RequirePermission(Permissions.FixedAssetRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("fixedasset", id);
            if (context is null)
                return NotFound(new { error = "Activo fijo no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Ficha de Activo Fijo");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Ficha de Activo Fijo' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "fixedasset");

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
            _logger.LogError(ex, "Error generando ficha de activo fijo {AssetId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
