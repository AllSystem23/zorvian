using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Gestión de suscripciones webhook. Permite a administradores registrar,
/// actualizar y eliminar webhooks, así como consultar los logs de entrega.
/// </summary>
[ApiController]
[Authorize(Roles = "SuperAdmin,CompanyAdmin")]
[Route("zorvian/v1/webhooks")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class WebhooksController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public WebhooksController(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Obtiene todas las suscripciones webhook registradas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WebhookSubscription>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subs = await _db.Set<WebhookSubscription>()
            .Where(s => s.TenantId == _tenant.TenantId)
            .ToListAsync();
        return Ok(subs);
    }

    /// <summary>
    /// Crea una nueva suscripción webhook. El secret se genera automáticamente.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Subscribe([FromBody] CreateWebhookRequest request)
    {
        var sub = new WebhookSubscription
        {
            TenantId = _tenant.TenantId ?? "",
            EventType = request.EventType,
            TargetUrl = request.TargetUrl,
            Secret = Guid.NewGuid().ToString("N"),
            Description = request.Description,
            MaxRetries = request.MaxRetries,
            RetryIntervalSeconds = request.RetryIntervalSeconds,
        };

        _db.Set<WebhookSubscription>().Add(sub);
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    /// <summary>
    /// Actualiza una suscripción webhook existente.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateWebhookRequest request)
    {
        var sub = await _db.Set<WebhookSubscription>().FindAsync(id);
        if (sub == null || sub.TenantId != _tenant.TenantId) return NotFound();

        sub.EventType = request.EventType;
        sub.TargetUrl = request.TargetUrl;
        sub.Description = request.Description;
        sub.IsActive = request.IsActive;
        sub.MaxRetries = request.MaxRetries;
        sub.RetryIntervalSeconds = request.RetryIntervalSeconds;

        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    /// <summary>
    /// Elimina una suscripción webhook.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var sub = await _db.Set<WebhookSubscription>().FindAsync(id);
        if (sub == null || sub.TenantId != _tenant.TenantId) return NotFound();

        _db.Set<WebhookSubscription>().Remove(sub);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Regenera el secret de una suscripción webhook. Invalida el anterior.
    /// </summary>
    [HttpPost("{id:guid}/regenerate-secret")]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegenerateSecret(Guid id)
    {
        var sub = await _db.Set<WebhookSubscription>().FindAsync(id);
        if (sub == null || sub.TenantId != _tenant.TenantId) return NotFound();

        sub.Secret = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    /// <summary>
    /// Obtiene los logs de entrega de webhooks, con paginación y filtro opcional por suscripción.
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(List<WebhookDeliveryLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeliveryLogs([FromQuery] Guid? subscriptionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.Set<WebhookDeliveryLog>()
            .Where(l => l.Subscription != null && l.Subscription.TenantId == _tenant.TenantId);

        if (subscriptionId.HasValue)
            query = query.Where(l => l.SubscriptionId == subscriptionId.Value);

        var logs = await query
            .OrderByDescending(l => l.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Obtiene los logs de entrega de una suscripción específica, con paginación.
    /// </summary>
    [HttpGet("{id:guid}/logs")]
    [ProducesResponseType(typeof(List<WebhookDeliveryLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var sub = await _db.Set<WebhookSubscription>().FindAsync(id);
        if (sub == null || sub.TenantId != _tenant.TenantId) return NotFound();

        var logs = await _db.Set<WebhookDeliveryLog>()
            .Where(l => l.SubscriptionId == id)
            .OrderByDescending(l => l.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(logs);
    }
}

public record CreateWebhookRequest(
    string EventType,
    string TargetUrl,
    string? Description,
    int MaxRetries = 3,
    int RetryIntervalSeconds = 60
);

public record UpdateWebhookRequest(
    string EventType,
    string TargetUrl,
    string? Description,
    bool IsActive = true,
    int MaxRetries = 3,
    int RetryIntervalSeconds = 60
);
