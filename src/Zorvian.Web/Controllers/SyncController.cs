using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/sync")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ITenantContext _tenant;

    public SyncController(ISyncService syncService, ITenantContext tenant)
    {
        _syncService = syncService;
        _tenant = tenant;
    }

    [HttpGet("pull")]
    [ProducesResponseType(typeof(SyncPullResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Pull(
        [FromQuery] string entity,
        [FromQuery] DateTime? since,
        [FromQuery] int take = 500)
    {
        if (string.IsNullOrWhiteSpace(entity))
            return BadRequest(new { error = "entity is required" });

        var result = await _syncService.PullAsync(
            new SyncPullRequest(entity, since, take), _tenant.TenantId.ToString());

        return Ok(result);
    }

    [HttpPost("push")]
    [ProducesResponseType(typeof(SyncPushResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Push([FromBody] List<SyncPushRequest> mutations)
    {
        if (mutations == null || mutations.Count == 0)
            return BadRequest(new { error = "mutations list is required" });

        var result = await _syncService.PushAsync(mutations, _tenant.TenantId.ToString());
        return Ok(result);
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Status()
    {
        return Ok(new
        {
            serverTime = DateTime.UtcNow,
            version = "1.0"
        });
    }
}
