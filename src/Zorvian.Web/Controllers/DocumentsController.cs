using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Document;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[Authorize]
[ApiController]
[Route("zorvian/v1/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _generationService;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IGeneratedDocumentRepository _docRepo;
    private readonly QuestPdfService _pdfService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentGenerationService generationService,
        IDocumentTemplateRepository templateRepo,
        IGeneratedDocumentRepository docRepo,
        QuestPdfService pdfService)
    {
        _documentService = documentService;
        _generationService = generationService;
        _templateRepo = templateRepo;
        _docRepo = docRepo;
        _pdfService = pdfService;
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _docRepo.GetAllPagedAsync(page, pageSize);
        return Ok(new PagedResult<GeneratedDocument>(items, total, page, pageSize));
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? category,
        [FromQuery] string? countryCode,
        [FromQuery] string? search,
        [FromQuery] string? module,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _templateRepo.GetFilteredAsync(
            category, countryCode, search, module, isActive, page, pageSize);
        return Ok(new PagedResult<DocumentTemplate>(items, total, page, pageSize));
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] DocumentTemplateDto dto)
    {
        var template = new DocumentTemplate
        {
            Name = dto.Name,
            Category = dto.Category,
            Content = dto.Content,
            CountryCode = dto.CountryCode,
            Module = dto.Module,
            IsActive = true,
            Variables = dto.Variables
        };
        await _templateRepo.AddAsync(template);
        await _templateRepo.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, template);
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplateById(Guid id)
    {
        var template = await _templateRepo.GetByIdAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string module,
        [FromQuery] string @event,
        [FromQuery] string countryCode = "NI")
    {
        var templates = await _documentService.SuggestTemplatesAsync(module, @event, countryCode);
        return Ok(templates);
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDocument([FromBody] GenerateDocumentRequest request)
    {
        var template = await _templateRepo.GetByIdAsync(request.TemplateId);
        if (template == null) return NotFound(new { detail = "Plantilla no encontrada" });

        var data = request.Variables;
        var doc = await _documentService.GenerateProfessionalDocumentAsync(request.TemplateId, request.EntityId, data);
        return Ok(doc);
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpPost("preview")]
    public async Task<IActionResult> PreviewTemplate([FromBody] PreviewRequest request)
    {
        string templateContent;

        if (!string.IsNullOrEmpty(request.Content))
        {
            templateContent = request.Content;
        }
        else if (request.TemplateId.HasValue)
        {
            var template = await _templateRepo.GetByIdAsync(request.TemplateId.Value);
            if (template == null) return NotFound(new { detail = "Plantilla no encontrada" });
            templateContent = template.Content;
        }
        else
        {
            return BadRequest(new { detail = "Se requiere templateId o content" });
        }

        var data = request.Variables ?? GetDefaultSampleData();
        var renderedHtml = await _documentService.RenderLiquidPreviewAsync(templateContent, data);
        return Ok(new { html = renderedHtml });
    }

    /// Obtiene el contexto de una entidad para previsualizar datos antes de generar.
    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("entity-context")]
    public async Task<IActionResult> GetEntityContext(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId)
    {
        var context = await _generationService.GetEntityContextAsync(entityType, entityId);
        if (context == null)
            return NotFound(new { detail = $"Entidad {entityType} con ID {entityId} no encontrada" });
        return Ok(context);
    }

    /// REGLA DE 3 CLICS: Genera cualquier documento en un solo paso.
    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("quick-generate")]
    public async Task<IActionResult> QuickGenerate([FromBody] QuickGenerateRequest request)
    {
        try
        {
            var result = await _generationService.QuickGenerateAsync(
                request.EntityType, request.EntityId, request.TemplateId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("quick-generate/employee-contract")]
    public async Task<IActionResult> QuickGenerateEmployeeContract([FromBody] QuickGenerateLegacyRequest request)
    {
        var result = await _generationService.QuickGenerateAsync(
            "employee", request.EntityId, request.TemplateId);
        return Ok(result);
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(Guid id)
    {
        var doc = await _documentService.GetDocumentDetailsAsync(id);
        if (doc == null) return NotFound();

        var templateName = doc.Template?.Name;
        var versions = doc.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionItem(v.VersionNumber, v.ChangesSummary, v.CreatedAt))
            .ToList();

        var signatures = doc.Signatures
            .Select(s => new DocumentSignatureItem(s.Id, s.SignerRole, s.Status, s.SignedAt))
            .ToList();

        return Ok(new DocumentDetailResponse(
            doc.Id, doc.Name, doc.EntityType, doc.Status,
            doc.CreatedAt, doc.Summary, templateName,
            versions, signatures));
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeRequest request)
    {
        await _documentService.FinalizeAndRequestSignatureAsync(id, request.Role, request.SignerId);
        return NoContent();
    }

    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return NotFound();

        var versions = await _docRepo.GetVersionsAsync(id);
        var latest = versions.MaxBy(v => v.VersionNumber);
        if (latest == null || string.IsNullOrEmpty(latest.Content))
            return NotFound(new { detail = "No hay contenido renderizado para este documento" });

        var pdfBytes = _pdfService.RenderHtmlToPdf(latest.Content);
        var fileName = $"{doc.Name}.pdf".Replace(" ", "_").Replace("/", "-");
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static Dictionary<string, string> GetDefaultSampleData()
    {
        return new Dictionary<string, string>
        {
            ["Company.Name"] = "Zorvian Corp S.A.",
            ["Company.Date"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["Company.TaxId"] = "J001234567-8",
            ["Employee.FullName"] = "Juan Carlos Perez",
            ["Employee.Position"] = "Gerente de Operaciones",
            ["Employee.Salary"] = "25,000.00",
            ["Employee.HireDate"] = "01/03/2024",
            ["Employee.Identification"] = "001-120590-0001X",
            ["Sale.Number"] = "VT-2024-0001",
            ["Sale.ClientName"] = "Maria Lopez",
            ["Sale.Total"] = "15,750.00",
            ["Sale.Date"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["client_name"] = "Maria Lopez",
            ["employee_name"] = "Juan Carlos Perez",
            ["employee_id"] = "001-120590-0001X",
            ["position"] = "Gerente de Operaciones",
            ["salary"] = "25,000.00",
            ["start_date"] = "01/03/2024",
            ["department"] = "Operaciones",
            ["candidate_name"] = "Ana Maria Rodriguez",
            ["grantor_name"] = "Pedro Sanchez",
            ["grantor_id"] = "001-150685-0002Y",
            ["attorney_name"] = "Laura Martinez",
            ["attorney_id"] = "001-200390-0003Z",
            ["powers"] = "Firmar contratos, representar legalmente y gestionar cuentas bancarias.",
            ["quote_number"] = "COT-2024-0001",
            ["quote_date"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["delivery_date"] = DateTime.Now.ToString("dd/MM/yyyy"),
            ["benefits"] = "Seguro medico, bono de productividad",
            ["payment_terms"] = "30 dias",
            ["notes"] = "Oferta sujeta a disponibilidad de inventario.",
            ["purpose"] = "Tramite bancario",
            ["carrier"] = "Transportes rapidos S.A.",
            ["received_by"] = "Roberto Díaz",
            ["invoice_ref"] = "FAC-2024-0042",
            ["validity_days"] = "15",
            ["scope"] = "Para gestionar asuntos legales y financieros.",
        };
    }
}

public record QuickGenerateLegacyRequest(Guid EntityId, Guid TemplateId);
public record FinalizeRequest(string Role, Guid SignerId);
public record DocumentTemplateDto(string Name, string Category, string Content, string CountryCode, string? Module, string? Variables);
public record GenerateDocumentRequest(Guid TemplateId, Guid EntityId, Dictionary<string, string> Variables);
public record PreviewRequest(Guid? TemplateId, string? Content, Dictionary<string, string>? Variables);
