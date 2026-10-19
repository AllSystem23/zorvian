using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Contadores de insignias del sidebar. Queda SIN permiso de módulo a
/// propósito: `nav_provider` lo consulta para TODOS los roles autenticados al
/// pintar la navegación, y un 403 dejaría el sidebar sin contadores.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/badges")]
public sealed class BadgesController : ControllerBase
{
    private readonly IBadgeService _service;

    public BadgesController(IBadgeService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBadges()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }
}
