using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Acceso a los tokens de restablecimiento de contraseña. La tabla es global
/// (sin filtro por compañía) porque el flujo es anónimo.
/// </summary>
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token);

    /// <summary>Busca un token por su hash SHA-256 (nunca se guarda el token en claro).</summary>
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash);

    /// <summary>
    /// Invalida los tokens pendientes de un usuario (queda invalidated = usado).
    /// No hace SaveChanges: el llamador decide cuándo confirmar.
    /// </summary>
    Task InvalidatePendingForUserAsync(Guid userId);

    Task SaveChangesAsync();
}
