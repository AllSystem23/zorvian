namespace Zorvian.Core.Entities;

/// <summary>
/// Token de un solo uso para restablecer la contraseña.
///
/// Tabla GLOBAL de credenciales: NO hereda <see cref="BaseEntity"/> a propósito
/// (es decir, sin <c>TenantId</c>) porque el flujo es anónimo — el solicitante
/// todavía no tiene compañía resuelta y el token ES la credencial. Filtrar
/// esta tabla por compañía dejaría el token fuera del alcance de quien lo
/// pidió, y la fila nunca se crearía para un usuario SuperAdmin sin
/// compañía seleccionada.
///
/// Solo se persiste el hash SHA-256 del token, nunca el token en claro.
/// </summary>
public sealed class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsUsed { get; set; }

    public bool IsExpired => ExpiresAt < DateTime.UtcNow;
}
