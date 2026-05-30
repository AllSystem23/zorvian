using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Data;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin,CompanyAdmin")]
[Route("api/v1/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly NexoraDbContext _db;
    private readonly ITenantContext _tenant;

    public WebhooksController(NexoraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subs = await _db.Set<WebhookSubscription>().ToListAsync();
        return Ok(subs);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] CreateWebhookRequest request)
    {
        var sub = new WebhookSubscription
        {
            EventType = request.EventType,
            TargetUrl = request.TargetUrl,
            Secret = Guid.NewGuid().ToString("N"),
            Description = request.Description
        };

        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var sub = await _db.Set<WebhookSubscription>().FindAsync(id);
        if (sub == null) return NotFound();

        _db.Set<WebhookSubscription>().Remove(sub);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateWebhookRequest(string EventType, string TargetUrl, string? Description);
