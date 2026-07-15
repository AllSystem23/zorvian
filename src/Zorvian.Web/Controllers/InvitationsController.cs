using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("zorvian/v1/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public InvitationsController(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var invitations = await _db.Invitations
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.Code,
                i.Email,
                i.Role,
                i.IsUsed,
                i.UsedAt,
                i.ExpiresAt,
                i.CreatedAt
            })
            .ToListAsync();
        return Ok(invitations);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request)
    {
        var invitation = new Invitation
        {
            Email = request.Email,
            Role = request.Role ?? "Employee",
            TenantId = _tenant.TenantId!,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();
        return Ok(invitation);
    }

    [HttpPost("{id}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid id)
    {
        var invitation = await _db.Invitations.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == _tenant.TenantId);
        if (invitation == null) return NotFound(new { error = "Invitación no encontrada" });

        invitation.Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.IsUsed = false;
        invitation.UsedAt = null;
        await _db.SaveChangesAsync();
        return Ok(invitation);
    }
}

public sealed record CreateInvitationRequest(string Email, string? Role);
