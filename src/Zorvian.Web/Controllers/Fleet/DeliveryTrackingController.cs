using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/deliveries")]
public sealed class DeliveryTrackingController : ControllerBase
{
    private readonly DeliveryTrackingService _service;

    public DeliveryTrackingController(DeliveryTrackingService service) => _service = service;

    /// <summary>Update delivery status (validates transitions).</summary>
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{deliveryId:guid}/status")]
    [ProducesResponseType(typeof(DeliveryTrackingResponse), 200)]
    public async Task<IActionResult> UpdateStatus(Guid deliveryId, [FromBody] DeliveryStatusUpdateRequest request)
    {
        try
        {
            var result = await _service.UpdateStatusAsync(deliveryId, request);
            if (result == null) return NotFound(new { error = "Delivery not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Confirm delivery with receiver info and evidence.</summary>
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("{deliveryId:guid}/confirm")]
    [ProducesResponseType(typeof(DeliveryTrackingResponse), 200)]
    public async Task<IActionResult> ConfirmDelivery(Guid deliveryId, [FromBody] ConfirmDeliveryRequest request)
    {
        try
        {
            var result = await _service.ConfirmDeliveryAsync(deliveryId, request);
            if (result == null) return NotFound(new { error = "Delivery not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get delivery tracking timeline (internal use).</summary>
    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{deliveryId:guid}/timeline")]
    [ProducesResponseType(typeof(DeliveryTrackingTimeline), 200)]
    public async Task<IActionResult> GetTimeline(Guid deliveryId)
    {
        var result = await _service.GetTrackingTimelineAsync(deliveryId);
        if (result == null) return NotFound(new { error = "Delivery not found" });
        return Ok(result);
    }

    /// <summary>Send ETA notification to client.</summary>
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost("{deliveryId:guid}/notify-eta")]
    [ProducesResponseType(typeof(EtaNotificationResponse), 200)]
    public async Task<IActionResult> SendEtaNotification(Guid deliveryId, [FromBody] EtaNotificationRequest? request = null)
    {
        var result = await _service.SendEtaNotificationAsync(
            new EtaNotificationRequest(deliveryId, request?.NotificationType ?? "push"));
        if (result == null) return NotFound(new { error = "Delivery not found" });
        return Ok(result);
    }

    /// <summary>Public client tracking endpoint (no auth required).</summary>
    [AllowAnonymous]
    [HttpGet("track/{code}")]
    [ProducesResponseType(typeof(ClientDeliveryTracking), 200)]
    public async Task<IActionResult> GetClientTracking(string code)
    {
        var result = await _service.GetClientTrackingAsync(code);
        if (result == null) return NotFound(new { error = "Delivery not found" });
        return Ok(result);
    }
}
