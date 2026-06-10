using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

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
