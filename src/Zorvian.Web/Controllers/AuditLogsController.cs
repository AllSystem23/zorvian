using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/audit")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repo;

    public AuditLogsController(IAuditLogRepository repo)
    {
        _repo = repo;
    }

    [HttpGet("logs")]
    [RequirePermission(Permissions.AuditRead)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var items = await _repo.GetFilteredAsync(entityName, action, from, to, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(entityName, action, from, to);

        return Ok(new
        {
            data = items.Select(a => new
            {
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.PerformedBy,
                a.IpAddress,
                a.RequestPath,
                a.CreatedAt,
            }),
            total,
            page,
            pageSize,
        });
    }
}
