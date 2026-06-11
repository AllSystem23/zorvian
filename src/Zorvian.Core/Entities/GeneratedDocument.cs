namespace Zorvian.Core.Entities;

/// <summary>
/// Representa un documento generado a partir de una plantilla.
/// </summary>
public sealed class GeneratedDocument : BaseEntity
{
    public Guid TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }
    
    public Guid EntityId { get; set; } // Id of the related record (Employee, Sale, etc.)
    public string EntityType { get; set; } = string.Empty; // Typename (Employee, Credit)
    
    public string Status { get; set; } = "draft"; // draft, pending, approved, signed, archived
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();
}
