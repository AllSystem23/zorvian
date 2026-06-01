using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly NexoraDbContext _db;
    private readonly ITenantContext _tenant;

    public InvitationsController(NexoraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request)
    {
        var invitation = new Invitation
        {
            Email = request.Email,
            Role = request.Role ?? "Employee",
            TenantId = _tenant.TenantId!
        };
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();
        return Ok(invitation);
    }
}

public sealed record CreateInvitationRequest(string Email, string? Role);
