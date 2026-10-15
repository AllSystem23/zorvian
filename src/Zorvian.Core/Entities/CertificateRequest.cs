namespace Zorvian.Core.Entities;

/// <summary>
/// Solicitud de constancia (salarial o laboral) emitida por un trabajador.
/// Flujo: el trabajador solicita → RRHH aprueba/rechaza → si aprueba, se genera el documento PDF.
/// </summary>
public sealed class CertificateRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Tipo de constancia solicitada: "salarial" o "laboral".</summary>
    public string CertificateType { get; set; } = "salarial";

    /// <summary>Motivo o propósito de la solicitud.</summary>
    public string? Reason { get; set; }

    /// <summary>Estado: "pending", "approved", "rejected".</summary>
    public string Status { get; set; } = "pending";

    public string? RejectionReason { get; set; }

    /// <summary>Id del documento generado al aprobar (PDF).</summary>
    public Guid? GeneratedDocumentId { get; set; }
    public GeneratedDocument? GeneratedDocument { get; set; }

    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
