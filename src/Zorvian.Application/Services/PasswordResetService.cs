using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// Flujo de restablecimiento de contraseña:
/// <list type="number">
///   <item><see cref="RequestResetAsync"/> genera un token de un solo uso, lo
///   persiste (hasheado) y envía el enlace por correo.</item>
///   <item><see cref="ResetAsync"/> valida el token, fija la contraseña nueva y
///   revoca las sesiones activas del usuario.</item>
/// </list>
/// El token NUNCA se guarda en claro: en la BD solo queda su hash SHA-256.
/// </summary>
public sealed class PasswordResetService
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IAuthRepository _authRepo;
    private readonly IFirebaseAuthService _firebase;
    private readonly IEmailService _email;
    private readonly IAppLinks _links;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IPasswordResetTokenRepository tokens,
        IAuthRepository authRepo,
        IFirebaseAuthService firebase,
        IEmailService email,
        IAppLinks links,
        ILogger<PasswordResetService> logger)
    {
        _tokens = tokens;
        _authRepo = authRepo;
        _firebase = firebase;
        _email = email;
        _links = links;
        _logger = logger;
    }

    /// <summary>
    /// Punto de entrada de "Olvidé mi contraseña". SIEMPRE devuelve <c>true</c>
    /// (salvo email vacío): el endpoint responde un mensaje genérico para no
    /// revelar qué correos existen. Los fallos de SMTP se registran pero no se
    /// propagan — si se propagaran, un correo registrado con SMTP caído
    /// devolvería 400 y uno no registrado 200 (fuga de enumeración).
    /// </summary>
    public async Task<bool> RequestResetAsync(string email)
    {
        var normalized = email?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return false;

        var user = await _authRepo.GetUserByEmailIgnoreCaseAsync(normalized);
        if (user is null)
        {
            _logger.LogWarning("Password reset requested for an email that is not registered");
            return true;
        }

        await _tokens.InvalidatePendingForUserAsync(user.Id);

        var rawToken = GenerateToken();
        await _tokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Email = user.Email,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_links.PasswordResetTokenMinutes)
        });
        await _tokens.SaveChangesAsync();

        var link = _links.BuildResetPasswordUrl(rawToken);
        var appLink = _links.BuildResetPasswordAppUrl(rawToken);

        try
        {
            await _email.SendEmailAsync(
                user.Email,
                "Recuperación de Contraseña — Zorvian ERP",
                BuildEmailHtml(link, appLink, _links.PasswordResetTokenMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the password reset email. The reset link is only valid if the email was delivered.");
        }

        _logger.LogInformation("Password reset token issued for user {UserId}", user.Id);
        return true;
    }

    /// <summary>
    /// Consume el token y fija la contraseña nueva. Revoca además las sesiones
    /// activas del usuario (los refresh tokens quedan invalidados).
    /// </summary>
    public async Task<(bool Success, string? Error)> ResetAsync(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "El enlace de restablecimiento no es válido.");

        var entity = await _tokens.GetByHashAsync(HashToken(token.Trim()));
        if (entity is null || entity.IsUsed)
            return (false, "El enlace de restablecimiento no es válido o ya fue usado.");

        if (entity.ExpiresAt < DateTime.UtcNow)
            return (false, "El enlace expiró. Solicita uno nuevo.");

        var user = await _authRepo.GetUserWithRolesAsync(entity.UserId);
        if (user is null)
            return (false, "No encontramos la cuenta asociada a este enlace.");

        if (!string.IsNullOrEmpty(user.FirebaseUid))
        {
            try
            {
                await _firebase.UpdatePasswordAsync(user.FirebaseUid, newPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update the Firebase password for user {UserId}", user.Id);
                return (false, "Error al actualizar la contraseña en el servidor de autenticación.");
            }
        }

        user.PasswordHash = PasswordHelper.Hash(newPassword);
        entity.IsUsed = true;
        entity.UsedAt = DateTime.UtcNow;

        await _tokens.InvalidatePendingForUserAsync(user.Id);
        await _tokens.SaveChangesAsync();
        await _authRepo.SaveChangesAsync();

        // Una recuperación de contraseña debe cerrar las sesiones activas.
        await _authRepo.RevokeAllUserTokensAsync(user.Id);
        await _authRepo.SaveChangesAsync();

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        return (true, null);
    }

    /// <summary>Token aleatorio de 256 bits en Base64Url.</summary>
    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>Hash SHA-256 (hex) del token: es lo único que se persiste.</summary>
    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }

    private static string BuildEmailHtml(string link, string appLink, int minutes) =>
        $"""
        <div style="font-family: 'Inter', sans-serif; max-width: 480px; margin: 0 auto;">
            <div style="background: linear-gradient(135deg, #1A0A3E, #7C4DFF); padding: 32px; border-radius: 16px 16px 0 0; text-align: center;">
                <h1 style="color: #fff; margin: 0; font-size: 24px;">Recuperación de Contraseña</h1>
            </div>
            <div style="background: #fff; padding: 32px; border-radius: 0 0 16px 16px; color: #333;">
                <p>Hemos recibido una solicitud para restablecer tu contraseña de <strong>Zorvian ERP</strong>.</p>
                <p>Pulsa el botón para elegir una contraseña nueva:</p>
                <p style="margin: 24px 0;">
                    <a href="{link}" style="background: #7C4DFF; color: #fff; text-decoration: none; padding: 12px 24px; border-radius: 8px; display: inline-block;">Restablecer contraseña</a>
                </p>
                <p style="margin: 16px 0;">
                    <a href="{appLink}" style="background: #1A0A3E; color: #fff; text-decoration: none; padding: 12px 24px; border-radius: 8px; display: inline-block;">Abrir en la app de Zorvian</a>
                </p>
                <p style="color: #666; font-size: 13px;">Este enlace vence en {minutes} minutos y solo puede usarse una vez.</p>
                <p style="color: #999; font-size: 12px; word-break: break-all;">Si el botón no funciona, copia esta dirección:<br />{link}</p>
                <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;" />
                <p style="color: #aaa; font-size: 12px; text-align: center;">Si no solicitaste este cambio, ignora este mensaje: tu contraseña seguirá siendo la misma.</p>
                <p style="color: #aaa; font-size: 12px; text-align: center;">© 2026 Zorvian ERP — Todos los derechos reservados.</p>
            </div>
        </div>
        """;
}
