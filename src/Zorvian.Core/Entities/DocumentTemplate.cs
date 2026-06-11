namespace Zorvian.Core.Entities;

/// <summary>
/// Define la estructura y variables de una plantilla documental.
/// </summary>
public sealed class DocumentTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // HR, Sales, Legal, etc.
    public string Content { get; set; } = string.Empty; // HTML or XML content
    public string CountryCode { get; set; } = string.Empty; // ISO code or "ALL"
    public string? Module { get; set; } // Integration module
    public bool IsActive { get; set; } = true;
    public string? Version { get; set; }
    
    public ICollection<GeneratedDocument> GeneratedDocuments { get; set; } = new List<GeneratedDocument>();
}
