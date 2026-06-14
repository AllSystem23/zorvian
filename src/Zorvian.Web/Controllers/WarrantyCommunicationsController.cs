using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/warranty-communications")]
public sealed class WarrantyCommunicationsController : ControllerBase
{
    private readonly WarrantyCommunicationService _service;

    public WarrantyCommunicationsController(WarrantyCommunicationService service) => _service = service;

    [RequirePermission(Permissions.WarrantyRead)]
    [HttpGet("by-warranty/{warrantyId:guid}")]
    public async Task<IActionResult> GetByWarranty(Guid warrantyId)
    {
        var items = await _service.GetByWarrantyIdAsync(warrantyId);
        return Ok(items);
    }

    [Audit("WarrantyCommunication", "Send")]
    [RequirePermission(Permissions.WarrantyWrite)]
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendWarrantyCommunicationRequest request)
    {
        var item = await _service.SendAsync(request);
        return CreatedAtAction(null, null, item);
    }
}
