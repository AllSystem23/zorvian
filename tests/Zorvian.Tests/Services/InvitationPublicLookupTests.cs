using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

/// <summary>
/// Regresión del bug de la invitación en el REGISTRO PÚBLICO.
///
/// La invitación se crea con el <c>TenantId</c> de la COMPAÑÍA DESTINO
/// (la que elige el SuperAdmin al aprobar la solicitud de acceso), pero el
/// registro es anónimo: el contexto no tiene compañía resuelta. Con el
/// <c>HasQueryFilter</c> por compañía, la fila quedaba fuera del alcance del
/// lookup por código y el registro con invitación respondía siempre
/// "Código de invitación inválido" — el flujo de "Solicitar Acceso" era
/// inalcanzable de punta a punta.
///
/// La invitación NO lleva filtro por compañía: el código es la credencial
/// (único) y las lecturas administrativas filtran por <c>TenantId</c>
/// explícitamente en la query.
/// </summary>
public sealed class InvitationPublicLookupTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly string _companyTenantId;

    public InvitationPublicLookupTests()
    {
        _companyTenantId = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Contexto anónimo: sin compañía resuelta (registro público).
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromGuid(Guid.Empty));
        _tenant.Setup(t => t.BypassTenantFilter).Returns(false);

        _db = new ZorvianDbContext(options, _tenant.Object);

        _db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            Code = "ABCD1234",
            Email = "nuevo@empresa.com",
            Role = "Employee",
            TenantId = _companyTenantId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task LookupByCode_FromAnonymousContext_FindsTheInvitation()
    {
        var invitation = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Code == "ABCD1234" && !i.IsUsed);

        Assert.NotNull(invitation);
        Assert.Equal("nuevo@empresa.com", invitation!.Email);
        Assert.Equal(_companyTenantId, invitation.TenantId);
    }

    [Fact]
    public async Task UsedInvitation_IsNotReturnedByTheLookup()
    {
        var invitation = await _db.Invitations.FirstAsync(i => i.Code == "ABCD1234");
        invitation.IsUsed = true;
        invitation.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var found = await _db.Invitations
            .FirstOrDefaultAsync(i => i.Code == "ABCD1234" && !i.IsUsed);

        Assert.Null(found);
    }
}
