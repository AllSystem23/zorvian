using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Revisión de las solicitudes de acceso: la cola es de plataforma, así que se
/// exige <c>company.manage</c> (permiso exclusivo de SuperAdmin).
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/access-requests")]
public sealed class AccessRequestsController : ControllerBase
{
    private readonly AccessRequestService _accessRequests;

    public AccessRequestsController(AccessRequestService accessRequests)
    {
        _accessRequests = accessRequests;
    }

    [HttpGet]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var items = await _accessRequests.ListAsync(status);
        return Ok(items);
    }

    [HttpPost("{id}/approve")]
    [RequirePermission(Permissions.CompanyManage)]
    [Audit("AccessRequest", "Approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveAccessRequest request)
    {
        var (success, error) = await _accessRequests.ApproveAsync(id, request.CompanyId, request.Role);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Solicitud aprobada. El código de invitación fue enviado por correo." });
    }

    [HttpPost("{id}/reject")]
    [RequirePermission(Permissions.CompanyManage)]
    [Audit("AccessRequest", "Reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAccessRequest? request)
    {
        var (success, error) = await _accessRequests.RejectAsync(id, request?.Reason);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Solicitud rechazada." });
    }
}

public sealed record ApproveAccessRequest(Guid CompanyId, string? Role);

public sealed record RejectAccessRequest(string? Reason);
