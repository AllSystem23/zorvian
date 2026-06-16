using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/dashboard")]
public sealed class FleetDashboardController : ControllerBase
{
    private readonly FleetDashboardService _service;

    public FleetDashboardController(FleetDashboardService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var data = await _service.GetDashboardAsync();
        return Ok(data);
    }
}
