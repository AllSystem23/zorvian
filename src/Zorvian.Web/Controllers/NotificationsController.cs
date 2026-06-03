using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Notifications;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de notificaciones. Administra el registro de dispositivos para notificaciones push.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ZorvianDbContext _db;

    public NotificationsController(ZorvianDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Registra o actualiza un dispositivo para recibir notificaciones push.
    /// </summary>
    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "User not authenticated" });

        var existing = await _db.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == request.Token);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.Platform = request.Platform;
        }
        else
        {
            _db.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Token = request.Token,
                Platform = request.Platform,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { registered = true });
    }
}
