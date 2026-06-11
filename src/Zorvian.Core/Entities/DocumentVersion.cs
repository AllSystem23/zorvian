namespace Zorvian.Core.Entities;

/// <summary>
/// Historial de versiones y archivos físicos de un documento.
/// </summary>
public sealed class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public GeneratedDocument? Document { get; set; }
    
    public int VersionNumber { get; set; }
    public string Content { get; set; } = string.Empty; // Rendered HTML/Liquid content
    public string FilePath { get; set; } = string.Empty; // Path in storage
    public string? FileHash { get; set; } // Integrity check
    public string? ChangesSummary { get; set; }
    public long FileSizeBytes { get; set; }
}
