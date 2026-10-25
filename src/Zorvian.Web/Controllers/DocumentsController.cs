using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Document;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
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
    private readonly ISignatureService _signatureService;
    private readonly ITenantContext _tenant;
    private readonly QuestPdfService _pdfService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentGenerationService generationService,
        IDocumentTemplateRepository templateRepo,
        IGeneratedDocumentRepository docRepo,
        ISignatureService signatureService,
        ITenantContext tenant,
        QuestPdfService pdfService)
    {
        _documentService = documentService;
        _generationService = generationService;
        _templateRepo = templateRepo;
        _docRepo = docRepo;
        _signatureService = signatureService;
        _tenant = tenant;
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
                request.EntityType, request.EntityId, request.TemplateId, request.Variables);
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

    [RequirePermission(Permissions.DocumentWrite)]
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocumentHard(Guid id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return NotFound(new { detail = "Documento no encontrado" });

        // Eliminar firmas asociadas
        var signatures = await _docRepo.GetSignaturesAsync(id);
        foreach (var signature in signatures)
        {
            _db.DocumentSignatures.Remove(signature);
        }
        
        // Also remove all versions
        var versions = await _docRepo.GetVersionsAsync(id);
        foreach (var version in versions)
        {
            _db.DocumentVersions.Remove(version);
        }
        
        // Remove the document itself
        _db.GeneratedDocuments.Remove(doc);
        
        await _docRepo.SaveChangesAsync();
        return Ok(new { message = "Documento eliminado permanentemente" });
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPut("{id}/soft-delete")]
    public async Task<IActionResult> DeleteDocumentSoft(Guid id)
    {
        var doc = await _docRepo.GetByIdAsync(id);
        if (doc == null) return NotFound(new { detail = "Documento no encontrado" });

        doc.IsDeleted = true;
        doc.DeletedAt = DateTime.UtcNow;
        await _docRepo.UpdateAsync(doc);
        await _docRepo.SaveChangesAsync();
        return Ok(new { message = "Documento marcado como eliminado" });
    }

    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> Finalize(Guid id, [FromBody] FinalizeRequest request)
    {
        await _documentService.FinalizeAndRequestSignatureAsync(id, request.Role, request.SignerId);
        return NoContent();
    }

    /// <summary>
    /// Firma digital del documento: fija la huella SHA-256 del HTML de la
    /// última versión en <c>SignatureData</c> y marca firma + documento como
    /// firmados. La misma huella la recalcula el endpoint público de
    /// verificación que codifica el QR del bloque de auditoría.
    /// </summary>
    [RequirePermission(Permissions.DocumentWrite)]
    [HttpPost("{id}/sign")]
    public async Task<IActionResult> Sign(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SignDocumentRequest? request)
    {
        try
        {
            var signature = await _signatureService.SignDocumentAsync(
                id,
                request?.Role ?? string.Empty,
                request?.Notes,
                _tenant.CurrentUserId?.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new DocumentSignatureItem(
                signature.Id, signature.SignerRole, signature.Status, signature.SignedAt));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
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

    /// <summary>
    /// Verificación PÚBLICA (anónima) del documento que codifica el QR del
    /// bloque de auditoría: recalcula el SHA-256 de la última versión y lo
    /// compara con la huella que se fijó al firmar.
    ///
    /// Devuelve una PÁGINA DE VERIFICACIÓN, no el contenido del documento: el
    /// QR viaja impreso en papel y exponer el cuerpo (salarios, montos de
    /// liquidación) a cualquiera con la URL sería una fuga. Por eso también
    /// IGNORA el query filter de compañía (una petición anónima no tiene
    /// tenant y el filtro normal devolvería 404 → QR roto).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("verify/{id:guid}")]
    public async Task<IActionResult> Verify(Guid id)
    {
        var doc = await _docRepo.GetByIdForPublicVerificationAsync(id);
        if (doc == null) return NotFound(new { detail = "Documento no encontrado" });

        var latest = doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latest == null || string.IsNullOrEmpty(latest.Content))
            return NotFound(new { detail = "Sin contenido renderizado" });

        var actualHash = DocumentContentHasher.Sha256Hex(latest.Content);
        var signed = doc.Signatures
            .Where(s => s.Status == "signed" && !string.IsNullOrEmpty(s.SignatureData))
            .ToList();
        var intact = signed.Any(s =>
            string.Equals(s.SignatureData, actualHash, StringComparison.OrdinalIgnoreCase));

        return Content(
            BuildVerificationHtml(doc, signed, actualHash, intact),
            "text/html; charset=utf-8");
    }

    /// <summary>
    /// Página de verificación autocontena (sin assets externos ni JS): la
    /// escanea el cliente del documento desde el QR.
    /// </summary>
    private static string BuildVerificationHtml(
        GeneratedDocument doc,
        IReadOnlyCollection<DocumentSignature> signed,
        string actualHash,
        bool intact)
    {
        static string Enc(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

        var (_, banner, color) = signed.Count == 0
            ? ("sin-firma", "Este documento NO está firmado", "#b26a00")
            : intact
                ? ("ok", "Verificación superada: la huella coincide", "#0a7d33")
                : ("bad", "ALERTA: la huella NO coincide. El contenido fue alterado después de la firma.", "#c62828");

        var rows = signed.Count == 0
            ? "<p>No hay firmas registradas para este documento.</p>"
            : "<ul>" + string.Join("", signed.Select(s =>
                $"<li><strong>{Enc(s.SignerRole)}</strong> · firmado el " +
                $"{(s.SignedAt?.ToString("dd/MM/yyyy HH:mm") ?? "—")} UTC · " +
                $"huella <code>{Enc(s.SignatureData)}</code></li>")) + "</ul>";

        return
            "<!doctype html><html lang='es'><head><meta charset='utf-8'>" +
            "<meta name='viewport' content='width=device-width, initial-scale=1'>" +
            "<title>Verificación de documento · Zorvian ERP</title></head>" +
            "<body style='font-family:Arial,Helvetica,sans-serif;background:#f5f6fa;color:#1a1a2e;margin:0;padding:24px;'>" +
            "<div style='max-width:640px;margin:0 auto;background:#fff;border:1px solid #e2e4ea;border-radius:12px;padding:24px;'>" +
            "<div style='font-size:11px;letter-spacing:2px;text-transform:uppercase;color:#7c4dff;'>Zorvian ERP · Document Engine</div>" +
            "<h1 style='font-size:20px;margin:8px 0 12px;'>Verificación de documento</h1>" +
            $"<div style='border-left:4px solid {color};background:{color}14;padding:12px 14px;border-radius:6px;font-weight:700;margin-bottom:18px;'>{Enc(banner)}</div>" +
            "<table style='border-collapse:collapse;width:100%;font-size:14px;'>" +
            $"<tr><td style='color:#6b6f80;padding:6px 0;'>Documento</td><td style='padding:6px 0;text-align:right;'><strong>{Enc(doc.Name)}</strong></td></tr>" +
            $"<tr><td style='color:#6b6f80;padding:6px 0;'>Tipo</td><td style='padding:6px 0;text-align:right;'>{Enc(doc.EntityType)}</td></tr>" +
            $"<tr><td style='color:#6b6f80;padding:6px 0;'>Generado el</td><td style='padding:6px 0;text-align:right;'>{doc.CreatedAt:dd/MM/yyyy HH:mm} UTC</td></tr>" +
            $"<tr><td style='color:#6b6f80;padding:6px 0;'>Estado</td><td style='padding:6px 0;text-align:right;'>{Enc(doc.Status)}</td></tr>" +
            "</table>" +
            "<h2 style='font-size:14px;margin:20px 0 6px;'>Firmas</h2>" + rows +
            "<h2 style='font-size:14px;margin:20px 0 6px;'>Huella SHA-256 del contenido</h2>" +
            $"<code style='display:block;word-break:break-all;font-size:12px;background:#f1f2f7;padding:8px;border-radius:6px;'>{Enc(actualHash)}</code>" +
            $"<p style='color:#6b6f80;font-size:12px;margin-top:18px;'>Verificado el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC · Zorvian ERP</p>" +
            "</div></body></html>";
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
public record SignDocumentRequest(string? Role, string? Notes);
public record DocumentTemplateDto(string Name, string Category, string Content, string CountryCode, string? Module, string? Variables);
public record GenerateDocumentRequest(Guid TemplateId, Guid EntityId, Dictionary<string, string> Variables);
public record PreviewRequest(Guid? TemplateId, string? Content, Dictionary<string, string>? Variables);
