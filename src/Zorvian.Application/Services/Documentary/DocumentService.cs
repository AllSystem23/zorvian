using Fluid;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.Documentary;

public sealed class DocumentService : IDocumentService
{
    private static readonly FluidParser _parser = new FluidParser();
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IGeneratedDocumentRepository _docRepo;
    private readonly IAiDocumentService _aiService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentTemplateRepository templateRepo,
        IGeneratedDocumentRepository docRepo,
        IAiDocumentService aiService,
        ILogger<DocumentService> logger)
    {
        _templateRepo = templateRepo;
        _docRepo = docRepo;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<List<DocumentTemplate>> SuggestTemplatesAsync(string module, string @event, string countryCode)
    {
        _logger.LogInformation("Suggesting templates for Module: {Module}, Event: {Event}, Country: {Country}", module, @event, countryCode);
        
        var templates = await _templateRepo.GetByCountryAsync(countryCode);
        return templates.Where(t => t.Module == module && t.IsActive).ToList();
    }

    public async Task<GeneratedDocument> GenerateProfessionalDocumentAsync(Guid templateId, Guid entityId, object variableData)
    {
        var template = await _templateRepo.GetByIdAsync(templateId);
        if (template == null) throw new KeyNotFoundException("Template not found");

        _logger.LogInformation("Generating professional document from template {TemplateName} for entity {EntityId}", template.Name, entityId);

        string renderedContent;
        try
        {
            renderedContent = await RenderLiquidTemplate(template.Content, variableData);
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
            EntityType = template.Module ?? "General",
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
        return await RenderLiquidTemplate(templateContent, variableData);
    }

    private async Task<string> RenderLiquidTemplate(string source, object model)
    {
        if (!_parser.TryParse(source, out var template, out var error))
        {
            throw new Exception($"Liquid syntax error: {error}");
        }

        var context = new TemplateContext(model);
        context.Options.MemberAccessStrategy.Register(model.GetType());
        
        return await template.RenderAsync(context);
    }
}
