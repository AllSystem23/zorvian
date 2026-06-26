using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranty-part-usages")]
public sealed class WarrantyPartUsagesController : ControllerBase
{
    private readonly WarrantyPartUsageService _service;

    public WarrantyPartUsagesController(WarrantyPartUsageService service)
    {
        _service = service;
    }

    [Audit("WarrantyPartUsage", "Create")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> RecordUsage(
        [FromBody] CreateWarrantyPartUsageRequest request,
        [FromQuery] Guid warrantyId,
        [FromQuery] Guid branchId)
    {
        try
        {
            var result = await _service.RecordUsageAsync(request, branchId, warrantyId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
