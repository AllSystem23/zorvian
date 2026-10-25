using Microsoft.Extensions.Configuration;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

/// <summary>
/// Configuración pública de la app web. Lee <c>App:FrontendBaseUrl</c> y
/// <c>App:PasswordResetTokenMinutes</c> de la configuración.
/// </summary>
public sealed class AppLinks : IAppLinks
{
    private const string DefaultFrontendBaseUrl = "https://zorvian-erp.web.app";
    private const int DefaultTokenMinutes = 30;
    private const string DefaultMobileAppScheme = "zorvian";

    private readonly IConfiguration _config;

    public AppLinks(IConfiguration config)
    {
        _config = config;
    }

    public string FrontendBaseUrl
    {
        get
        {
            var configured = _config["App:FrontendBaseUrl"];
            var value = string.IsNullOrWhiteSpace(configured) ? DefaultFrontendBaseUrl : configured.Trim();
            return value.TrimEnd('/');
        }
    }

    public int PasswordResetTokenMinutes
    {
        get
        {
            var configured = _config["App:PasswordResetTokenMinutes"];
            return int.TryParse(configured, out var minutes) && minutes > 0
                ? minutes
                : DefaultTokenMinutes;
        }
    }

    public string BuildResetPasswordUrl(string token)
    {
        return $"{FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Base pública de la API (incluye <c>/zorvian/v1</c>), configurable con
    /// <c>App:PublicApiBaseUrl</c>. Vacío si no está configurada.
    /// </summary>
    public string PublicApiBaseUrl
    {
        get
        {
            var configured = _config["App:PublicApiBaseUrl"];
            return string.IsNullOrWhiteSpace(configured) ? string.Empty : configured.Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// URL absoluta de verificación de un documento (<c>GET /documents/verify/{id}</c>),
    /// la que codifica el QR del bloque de auditoría. Vacío sin base pública.
    /// </summary>
    public string BuildDocumentVerifyUrl(Guid documentId)
    {
        var baseUrl = PublicApiBaseUrl;
        return baseUrl.Length == 0 ? string.Empty : $"{baseUrl}/documents/verify/{documentId}";
    }

    /// <summary>
    /// Esquema de los deep links de la app móvil. Configurable con
    /// <c>App:MobileAppScheme</c> (por defecto <c>zorvian</c>).
    /// </summary>
    public string MobileAppScheme
    {
        get
        {
            var configured = _config["App:MobileAppScheme"];
            var value = string.IsNullOrWhiteSpace(configured) ? DefaultMobileAppScheme : configured.Trim();
            return value.TrimEnd(':');
        }
    }

    /// <summary>
    /// Deep link a la app móvil para restablecer la contraseña. El token
    /// viaja en la query string con el MISMO nombre que la ruta web
    /// (<c>token</c>), así el frontend mapea ambos orígenes al mismo contrato.
    /// </summary>
    public string BuildResetPasswordAppUrl(string token)
    {
        return $"{MobileAppScheme}://reset-password?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>
    /// Deep link a la app móvil para registrarse con una invitación. El código
    /// viaja como <c>code</c>, igual que en la ruta web <c>/register</c>.
    /// </summary>
    public string BuildInvitationAppUrl(string invitationCode)
    {
        return $"{MobileAppScheme}://register?code={Uri.EscapeDataString(invitationCode)}";
    }
}
