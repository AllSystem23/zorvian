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
}
