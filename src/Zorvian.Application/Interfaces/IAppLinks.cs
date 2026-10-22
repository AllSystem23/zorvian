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

    /// <summary>
    /// Esquema de los deep links de la app móvil (p. ej. <c>zorvian</c>).
    /// Configurable con <c>App:MobileAppScheme</c>.
    /// </summary>
    string MobileAppScheme { get; }

    /// <summary>
    /// Deep link que abre la app móvil directamente en la pantalla de
    /// restablecimiento de contraseña. El token viaja en la query string
    /// (mismo contrato que la ruta web <c>/reset-password</c>).
    /// </summary>
    string BuildResetPasswordAppUrl(string token);

    /// <summary>
    /// Deep link que abre la app móvil en la pantalla de registro con el
    /// código de invitación precargado (ruta web <c>/register?code=…</c>).
    /// </summary>
    string BuildInvitationAppUrl(string invitationCode);
}
