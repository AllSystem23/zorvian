using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

/// <summary>
/// Solicitud de acceso enviada desde el modo "Solicitar Acceso" del registro
/// público.
///
/// Tabla GLOBAL (cola de la plataforma): NO hereda <see cref="BaseEntity"/> porque
/// quien la envía todavía no pertenece a ninguna compañía — no hay
/// <c>TenantId</c> que asignar. La revisión la hace un administrador de
/// plataforma, que la convierte en una <see cref="Invitation"/> ligada a la
/// compañía destino.
/// </summary>
public sealed class AccessRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = AccessRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? InvitationCode { get; set; }
}
