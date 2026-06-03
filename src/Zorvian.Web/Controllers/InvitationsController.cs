using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public InvitationsController(ZorvianDbContext db, ITenantContext tenant)
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
