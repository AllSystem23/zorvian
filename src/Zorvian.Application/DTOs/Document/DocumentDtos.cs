namespace Zorvian.Application.DTOs.Document;

public sealed record QuickGenerateRequest(
    string EntityType,
    Guid EntityId,
    Guid TemplateId
);

public sealed record QuickGenerateResult(
    Guid DocumentId,
    string Name,
    string Status,
    DateTime CreatedAt,
    string? PdfUrl,
    string? SignatureToken
);

public sealed record EntityContextResponse(
    string EntityType,
    Guid EntityId,
    string DisplayName,
    Dictionary<string, string> Data
);

public sealed record DocumentDetailResponse(
    Guid Id,
    string Name,
    string EntityType,
    string Status,
    DateTime CreatedAt,
    string? Summary,
    string? TemplateName,
    List<DocumentVersionItem> Versions,
    List<DocumentSignatureItem> Signatures
);

public sealed record DocumentVersionItem(
    int VersionNumber,
    string? ChangesSummary,
    DateTime CreatedAt
);

public sealed record DocumentSignatureItem(
    Guid Id,
    string SignerRole,
    string Status,
    DateTime? SignedAt
);
