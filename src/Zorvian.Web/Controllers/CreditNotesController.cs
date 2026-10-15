using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/credit-notes")]
public sealed class CreditNotesController : ControllerBase
{
    private readonly CreditNoteService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<CreditNotesController> _logger;

    public CreditNotesController(
        CreditNoteService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<CreditNotesController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.CreditRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [RequirePermission(Permissions.CreditRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { error = "Credit note not found" });
        return Ok(item);
    }

    [RequirePermission(Permissions.CreditRead)]
    [HttpGet("by-sale/{saleId:guid}")]
    public async Task<IActionResult> GetBySaleId(Guid saleId)
    {
        var items = await _service.GetBySaleIdAsync(saleId);
        return Ok(items);
    }

    [Audit("CreditNote", "Create")]
    [RequirePermission(Permissions.CreditWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditNoteRequest request)
    {
        try
        {
            var creditNote = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = creditNote.Id }, creditNote);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("company first", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = ex.Message, redirectTo = "/onboarding" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("CreditNote", "GeneratePdf")]
    [RequirePermission(Permissions.CreditRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("creditnote", id);
            if (context is null)
                return NotFound(new { error = "Nota de crédito no encontrada" });

            var template = await _templateRepo.GetByNameAsync("Nota de Crédito");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Nota de Crédito' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "creditnote");

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
            _logger.LogError(ex, "Error generando PDF de nota de crédito {CreditNoteId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
