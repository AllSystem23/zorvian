using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/purchases/recommendations")]
public sealed class PurchaseRecommendationController : ControllerBase
{
    private readonly PurchaseRecommendationService _service;

    public PurchaseRecommendationController(PurchaseRecommendationService service)
    {
        _service = service;
    }

    [HttpGet]
    [RequirePermission(Permissions.InventoryRead)]
    public async Task<IActionResult> GetAll([FromQuery] int demandDays = 30, [FromQuery] int leadTimeDays = 7)
    {
        var result = await _service.GetRecommendationsAsync(demandDays, leadTimeDays);
        return Ok(result);
    }
}
