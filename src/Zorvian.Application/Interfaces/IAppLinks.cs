namespace Zorvian.Application.Interfaces;

/// <summary>
/// Configuración pública de la app web (frontend). Vive en Application para
/// que los servicios de dominio puedan armar enlaces sin depender de
/// <c>IConfiguration</c>.
/// </summary>
public interface IAppLinks
{
    /// <summary>Base pública del frontend, sin barra final.</summary>
    string FrontendBaseUrl { get; }

    /// <summary>Vigencia (minutos) del enlace de restablecimiento de contraseña.</summary>
    int PasswordResetTokenMinutes { get; }

    /// <summary>Enlace completo de restablecimiento (con el token en claro, de un solo uso).</summary>
    string BuildResetPasswordUrl(string token);
}
