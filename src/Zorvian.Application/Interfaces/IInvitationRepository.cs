using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Invitaciones de usuario por compañía. La invitación es el puente entre la
/// solicitud de acceso y el registro del usuario.
/// </summary>
public interface IInvitationRepository
{
    /// <summary>Crea una invitación (código único) para la compañía indicada.</summary>
    Task<Invitation> CreateAsync(string email, string? role, string tenantId);

    Task SaveChangesAsync();
}
