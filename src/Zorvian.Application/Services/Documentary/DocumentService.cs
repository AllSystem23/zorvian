using Fluid;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Documentary;

public sealed class DocumentService : IDocumentService
{
    private static readonly FluidParser _parser = new FluidParser();
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IGeneratedDocumentRepository _docRepo;
    private readonly IAiDocumentService _aiService;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentTemplateRepository templateRepo,
        IGeneratedDocumentRepository docRepo,
        IAiDocumentService aiService,
        ICompanyRepository companyRepo,
        ITenantContext tenant,
        ILogger<DocumentService> logger)
    {
        _templateRepo = templateRepo;
        _docRepo = docRepo;
        _aiService = aiService;
        _companyRepo = companyRepo;
        _tenant = tenant;
        _logger = logger;
    }

    /// <summary>
    /// Base Company.*/Country.*/Contract.City bajo las variables recibidas (el usuario manda).
    /// Así el wizard manual también renderiza datos multi-país sin pasarlos a mano.
    /// Acepta Dictionary&lt;string,string&gt; (contextos clásicos) y Dictionary&lt;string,object&gt;
    /// (contextos con listas de ítems tabulados para {% for %} en Liquid).
    /// </summary>
    private async Task<Dictionary<string, object>> WithCompanyContextAsync(object model)
    {
        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        var baseContext = DocumentCompanyContext.Build(company);
        var merged = new Dictionary<string, object>(baseContext.Count, StringComparer.Ordinal);

        foreach (var (key, value) in baseContext)
            merged[key] = value;

        switch (model)
        {
            case Dictionary<string, string> vars:
                foreach (var (key, value) in vars)
                    merged[key] = value;
                break;
            case Dictionary<string, object> varsObj:
                foreach (var (key, value) in varsObj)
                    merged[key] = value;
                break;
        }

        return merged;
    }

    public async Task<List<DocumentTemplate>> SuggestTemplatesAsync(string module, string @event, string countryCode)
    {
        _logger.LogInformation("Suggesting templates for Module: {Module}, Event: {Event}, Country: {Country}", module, @event, countryCode);
        
        var templates = await _templateRepo.GetByCountryAsync(countryCode);
        return templates.Where(t => t.Module == module && t.IsActive).ToList();
    }

    public async Task<GeneratedDocument> GenerateProfessionalDocumentAsync(Guid templateId, Guid entityId, object variableData, string? entityType = null)
    {
        var template = await _templateRepo.GetByIdAsync(templateId);
        if (template == null) throw new KeyNotFoundException("Template not found");

        _logger.LogInformation("Generating professional document from template {TemplateName} for entity {EntityId}", template.Name, entityId);

        string renderedContent;
        try
        {
            var model = await WithCompanyContextAsync(variableData);
            renderedContent = await RenderLiquidTemplate(template.Content, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering Liquid template for {TemplateName}", template.Name);
            throw new InvalidOperationException("Failed to render professional document template.", ex);
        }

        var doc = new GeneratedDocument
        {
            TemplateId = templateId,
            EntityId = entityId,
            EntityType = !string.IsNullOrWhiteSpace(entityType) ? entityType : template.Module ?? "General",
            Name = $"{template.Name} - {DateTime.UtcNow:yyyyMMdd_HHmm}",
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };

        await _docRepo.AddAsync(doc);
        
        var version = new DocumentVersion
        {
            DocumentId = doc.Id,
            VersionNumber = 1,
            Content = renderedContent,
            FilePath = $"documents/{doc.Id}/v1.html",
            CreatedAt = DateTime.UtcNow,
            ChangesSummary = "Initial professional generation"
        };

        await _docRepo.AddVersionAsync(version);
        await _docRepo.SaveChangesAsync();

        return doc;
    }

    public async Task FinalizeAndRequestSignatureAsync(Guid documentId, string signerRole, Guid signerId)
    {
        var doc = await _docRepo.GetByIdAsync(documentId);
        if (doc == null) throw new KeyNotFoundException("Document not found");

        _logger.LogInformation("Finalizing document {DocumentId} and requesting signature from {SignerId}", documentId, signerId);

        var versions = await _docRepo.GetVersionsAsync(documentId);
        var latestVersion = versions.MaxBy(v => v.VersionNumber);
        if (latestVersion != null)
        {
            try
            {
                var summary = await _aiService.SummarizeDocumentAsync(latestVersion.Content);
                if (!string.IsNullOrEmpty(summary))
                {
                    doc.Summary = summary;
                    _logger.LogInformation("AI summary generated for document {DocumentId}", documentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate AI summary for document {DocumentId}, continuing without summary", documentId);
            }
        }

        doc.Status = "pending_signature";
        
        var signature = new DocumentSignature
        {
            DocumentId = documentId,
            SignerType = "User",
            SignerRole = signerRole,
            SignerId = signerId.ToString(),
            Status = "pending",
            SignatureToken = Guid.NewGuid().ToString().Replace("-", ""),
            CreatedAt = DateTime.UtcNow
        };

        await _docRepo.AddSignatureAsync(signature);
        await _docRepo.UpdateAsync(doc);
        await _docRepo.SaveChangesAsync();
    }

    public async Task<GeneratedDocument?> GetDocumentDetailsAsync(Guid documentId)
    {
        return await _docRepo.GetByIdAsync(documentId);
    }

    public async Task<string> RenderLiquidPreviewAsync(string templateContent, object variableData)
    {
        var model = await WithCompanyContextAsync(variableData);
        return await RenderLiquidTemplate(templateContent, model);
    }

    private async Task<string> RenderLiquidTemplate(string source, object model)
    {
        // Inyección universal (no toca las plantillas): logo en el encabezado y
        // bloque de auditoría al final, aplicables a todos los documentos generados.
        // Se inyecta en el source ANTES de renderizar para que los placeholders Liquid se resuelvan.
        source = source.Replace(
            "<div class='header'>",
            "<div class='header'>\n{{% if Company.Logo != blank %}}<img src='{{{{ Company.Logo }}}}' alt='Logo' style='height:60px;margin-bottom:10px;' />{{% endif %}}",
            StringComparison.Ordinal);
        if (source.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            var audit = "<div class='audit-block'><strong>Bloque de auditoría:</strong> Documento generado el {{{{ Company.Date }}}} · Versión 2.1 · País: {{{{ Country.Name }}}} · Empresa: {{{{ Company.LegalName }}}} ({{{{ Company.TaxId }}}}) · Generado por Zorvian ERP · Document Engine<br/><img src='https://api.qrserver.com/v1/create-qr-code/?size=80x80&data={{{{ Company.TaxId }}}}-{{{{ Company.Date }}}}' style='width:80px;height:80px;border:1px solid #ddd;' alt='QR de verificación' /><br/><em>Términos y condiciones: documento válido únicamente con firma y sello de la empresa. Ley aplicable: {{{{ Country.LaborLaw }}}}.</em></div>";
            source = source.Replace("</body>", audit + "\n</body>", StringComparison.OrdinalIgnoreCase);
        }

        if (!_parser.TryParse(source, out var template, out var error))
        {
            throw new Exception($"Liquid syntax error: {error}");
        }

        // Las claves con punto (Company.Name, Country.WeeklyHours) deben anidarse:
        // Fluid resuelve acceso anidado, no claves planas con punto.
        var normalized = LiquidModelNormalizer.Normalize(model);
        var context = new TemplateContext(normalized);
        context.Options.MemberAccessStrategy.Register(normalized.GetType());

        var rendered = await template.RenderAsync(context);
        return rendered;
    }
}
