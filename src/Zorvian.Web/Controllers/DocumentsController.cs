using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
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

    public DocumentsController(
        IDocumentService documentService,
        IDocumentGenerationService generationService,
        IDocumentTemplateRepository templateRepo)
    {
        _documentService = documentService;
        _generationService = generationService;
        _templateRepo = templateRepo;
    }

    /// <summary>
    /// Lista plantillas con paginación y filtros.
    /// </summary>
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

    /// <summary>
    /// Crea una nueva plantilla.
    /// </summary>
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

    /// <summary>
    /// Obtiene una plantilla por ID.
    /// </summary>
    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplateById(Guid id)
    {
        var template = await _templateRepo.GetByIdAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    /// <summary>
    /// Sugiere plantillas para un módulo específico.
    /// </summary>
    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string module, [FromQuery] string @event, [FromQuery] string countryCode = "NI")
    {
        var templates = await _documentService.SuggestTemplatesAsync(module, @event, countryCode);
        return Ok(templates);
    }

    /// <summary>
    /// Genera un documento desde cualquier plantilla con variables dinámicas.
    /// </summary>
    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateDocument([FromBody] GenerateDocumentRequest request)
    {
        var template = await _templateRepo.GetByIdAsync(request.TemplateId);
        if (template == null) return NotFound(new { detail = "Plantilla no encontrada" });

        // Flatten variables directly so Liquid templates use {{ client_name }} not {{ Vars.client_name }}
        var data = request.Variables;
        var doc = await _documentService.GenerateProfessionalDocumentAsync(request.TemplateId, request.EntityId, data);
        return Ok(doc);
    }

    /// <summary>
    /// REGLA DE 3 CLICS: Genera un contrato de empleado y lo deja listo para firma.
    /// </summary>
    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("quick-generate/employee-contract")]
    public async Task<IActionResult> QuickGenerateEmployeeContract([FromBody] QuickGenerateRequest request)
    {
        var doc = await _generationService.QuickGenerateEmployeeContractAsync(request.EntityId, request.TemplateId);
        return Ok(doc);
    }

    /// <summary>
    /// Obtiene los detalles de un documento, incluyendo versiones y estado de firmas.
    /// </summary>
    [RequirePermission(Permissions.DocumentRead)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetails(Guid id)
    {
        var doc = await _documentService.GetDocumentDetailsAsync(id);
        if (doc == null) return NotFound();
        return Ok(doc);
    }

    /// <summary>
    /// Un solo click para finalizar y solicitar firma si el documento está en draft.
    /// </summary>
    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeRequest request)
    {
        await _documentService.FinalizeAndRequestSignatureAsync(id, request.Role, request.SignerId);
        return NoContent();
    }
}

public record QuickGenerateRequest(Guid EntityId, Guid TemplateId);
public record FinalizeRequest(string Role, Guid SignerId);
public record DocumentTemplateDto(string Name, string Category, string Content, string CountryCode, string? Module, string? Variables);
public record GenerateDocumentRequest(Guid TemplateId, Guid EntityId, Dictionary<string, string> Variables);
