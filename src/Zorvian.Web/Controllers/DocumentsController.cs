using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _generationService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentGenerationService generationService)
    {
        _documentService = documentService;
        _generationService = generationService;
    }

    /// <summary>
    /// Sugiere plantillas para un módulo específico.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string module, [FromQuery] string @event, [FromQuery] string countryCode = "NI")
    {
        var templates = await _documentService.SuggestTemplatesAsync(module, @event, countryCode);
        return Ok(templates);
    }

    /// <summary>
    /// REGLA DE 3 CLICS: Genera un contrato de empleado y lo deja listo para firma.
    /// </summary>
    [HttpPost("quick-generate/employee-contract")]
    public async Task<IActionResult> QuickGenerateEmployeeContract([FromBody] QuickGenerateRequest request)
    {
        var doc = await _generationService.QuickGenerateEmployeeContractAsync(request.EntityId, request.TemplateId);
        return Ok(doc);
    }

    /// <summary>
    /// Obtiene los detalles de un documento, incluyendo versiones y estado de firmas.
    /// </summary>
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
    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeRequest request)
    {
        await _documentService.FinalizeAndRequestSignatureAsync(id, request.Role, request.SignerId);
        return NoContent();
    }
}

public record QuickGenerateRequest(Guid EntityId, Guid TemplateId);
public record FinalizeRequest(string Role, Guid SignerId); // FIXED: Guid SignerId
