using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Flujo de "Solicitar Acceso": el solicitante público deja sus datos, un
/// administrador de plataforma la revisa y la convierte en una
/// <see cref="Invitation"/> (con su código) para la compañía destino.
/// </summary>
public sealed class AccessRequestService
{
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAccessRequestRepository _requests;
    private readonly IInvitationRepository _invitations;
    private readonly ICompanyRepository _companies;
    private readonly IEmailService _email;
    private readonly ITenantContext _tenant;
    private readonly ILogger<AccessRequestService> _logger;

    public AccessRequestService(
        IAccessRequestRepository requests,
        IInvitationRepository invitations,
        ICompanyRepository companies,
        IEmailService email,
        ITenantContext tenant,
        ILogger<AccessRequestService> logger)
    {
        _requests = requests;
        _invitations = invitations;
        _companies = companies;
        _email = email;
        _tenant = tenant;
        _logger = logger;
    }

    /// <summary>
    /// Registra la solicitud. Devuelve el mismo resultado si ya existía una
    /// pendiente para ese correo (no se duplica ni se filtra información).
    /// </summary>
    public async Task<(bool Success, string? Error)> RequestAsync(RequestAccessRequest request)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;

        if (fullName.Length == 0)
            return (false, "El nombre es requerido");

        if (email.Length == 0 || !EmailPattern.IsMatch(email))
            return (false, "Ingresa un correo válido");

        if (await _requests.ExistsPendingByEmailAsync(email))
        {
            _logger.LogInformation("Access request already pending for email {Email}", email);
            return (true, null);
        }

        var entity = new AccessRequest
        {
            FullName = fullName,
            Email = email,
            Company = request.Company?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim(),
            Message = request.Message?.Trim(),
            Status = AccessRequestStatus.Pending
        };

        await _requests.AddAsync(entity);
        await _requests.SaveChangesAsync();

        try
        {
            await _email.SendEmailAsync(
                email,
                "Solicitud de acceso recibida — Zorvian ERP",
                BuildAcknowledgementHtml(fullName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the access request acknowledgement email");
        }

        _logger.LogInformation("Access request registered for email {Email}", email);
        return (true, null);
    }

    public async Task<List<AccessRequest>> ListAsync(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status) && !AccessRequestStatus.IsValid(status))
            return [];

        return await _requests.GetAllAsync(status?.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Aprueba la solicitud y emite la invitación con su código. La
    /// <paramref name="companyId"/> es la compañía donde se va a registrar el
    /// usuario.
    /// </summary>
    public async Task<(bool Success, string? Error)> ApproveAsync(Guid id, Guid companyId, string? role)
    {
        if (companyId == Guid.Empty)
            return (false, "La compañía destino es requerida");

        var entity = await _requests.GetByIdAsync(id);
        if (entity is null)
            return (false, "Solicitud no encontrada");

        if (entity.Status != AccessRequestStatus.Pending)
            return (false, "La solicitud ya fue revisada");

        var company = await _companies.GetByIdForPlatformAsync(companyId);
        if (company is null)
            return (false, "La compañía indicada no existe");

        var invitation = await _invitations.CreateAsync(entity.Email, role, company.TenantId);

        entity.Status = AccessRequestStatus.Approved;
        entity.InvitationCode = invitation.Code;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedBy = _tenant.CurrentUserId?.ToString();

        await _requests.SaveChangesAsync();
        await _invitations.SaveChangesAsync();

        try
        {
            await _email.SendEmailAsync(
                entity.Email,
                "Acceso aprobado — Zorvian ERP",
                BuildApprovalHtml(entity.FullName, invitation.Code, company.Name));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not send the access approval email");
        }

        _logger.LogInformation("Access request {Id} approved with invitation {Code}", entity.Id, invitation.Code);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(Guid id, string? reason)
    {
        var entity = await _requests.GetByIdAsync(id);
        if (entity is null)
            return (false, "Solicitud no encontrada");

        if (entity.Status != AccessRequestStatus.Pending)
            return (false, "La solicitud ya fue revisada");

        entity.Status = AccessRequestStatus.Rejected;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedBy = _tenant.CurrentUserId?.ToString();
        entity.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _requests.SaveChangesAsync();

        _logger.LogInformation("Access request {Id} rejected", entity.Id);
        return (true, null);
    }

    private static string BuildAcknowledgementHtml(string fullName) =>
        $"""
        <div style="font-family: 'Inter', sans-serif; max-width: 480px; margin: 0 auto;">
            <div style="background: linear-gradient(135deg, #1A0A3E, #7C4DFF); padding: 32px; border-radius: 16px 16px 0 0; text-align: center;">
                <h1 style="color: #fff; margin: 0; font-size: 24px;">Solicitud recibida</h1>
            </div>
            <div style="background: #fff; padding: 32px; border-radius: 0 0 16px 16px; color: #333;">
                <p>Hola {fullName},</p>
                <p>Recibimos tu solicitud de acceso a <strong>Zorvian ERP</strong>. Nuestro equipo la revisará y te escribirá
                   con el código de invitación para que puedas registrarte.</p>
                <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;" />
                <p style="color: #aaa; font-size: 12px; text-align: center;">© 2026 Zorvian ERP — Todos los derechos reservados.</p>
            </div>
        </div>
        """;

    private static string BuildApprovalHtml(string fullName, string code, string companyName) =>
        $"""
        <div style="font-family: 'Inter', sans-serif; max-width: 480px; margin: 0 auto;">
            <div style="background: linear-gradient(135deg, #1A0A3E, #7C4DFF); padding: 32px; border-radius: 16px 16px 0 0; text-align: center;">
                <h1 style="color: #fff; margin: 0; font-size: 24px;">Acceso aprobado</h1>
            </div>
            <div style="background: #fff; padding: 32px; border-radius: 0 0 16px 16px; color: #333;">
                <p>Hola {fullName},</p>
                <p>Tu solicitud de acceso a <strong>Zorvian ERP</strong> fue aprobada para <strong>{companyName}</strong>.</p>
                <p>Tu código de invitación es:</p>
                <div style="background: #f5f5f5; padding: 16px; border-radius: 8px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #1A0A3E;">
                    {code}
                </div>
                <p style="margin-top: 16px; color: #666;">Ingresa con este código en la pantalla de registro de Zorvian ERP.</p>
                <hr style="border: none; border-top: 1px solid #eee; margin: 24px 0;" />
                <p style="color: #aaa; font-size: 12px; text-align: center;">© 2026 Zorvian ERP — Todos los derechos reservados.</p>
            </div>
        </div>
        """;
}
