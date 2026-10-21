using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

/// <summary>
/// Invitaciones de usuario. El <c>TenantId</c> se fija explícitamente porque la
/// invitación puede destinasse a una compañía distinta a la del usuario que la
/// emite (SuperAdmin revisando una solicitud de acceso).
/// </summary>
public sealed class InvitationRepository : IInvitationRepository
{
    private readonly ZorvianDbContext _db;

    public InvitationRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Invitation> CreateAsync(string email, string? role, string tenantId)
    {
        var invitation = new Invitation
        {
            Email = email,
            Role = string.IsNullOrWhiteSpace(role) ? "Employee" : role,
            TenantId = tenantId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _db.Invitations.AddAsync(invitation);
        return invitation;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
