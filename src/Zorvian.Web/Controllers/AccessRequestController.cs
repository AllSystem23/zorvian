using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Modo "Solicitar Acceso" del registro público. El solicitante deja sus datos
/// y un administrador de plataforma los revisa, emitiendo una invitación.
/// </summary>
[ApiController]
[Route("zorvian/v1/auth")]
public sealed class AccessRequestController : ControllerBase
{
    private readonly AccessRequestService _accessRequests;

    public AccessRequestController(AccessRequestService accessRequests)
    {
        _accessRequests = accessRequests;
    }

    [HttpPost("request-access")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestAccess([FromBody] RequestAccessRequest request)
    {
        var (success, error) = await _accessRequests.RequestAsync(request);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { message = "Recibimos tu solicitud de acceso. Te escribiremos cuando sea aprobada." });
    }
}
