using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Biometrics;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador biométrico. Administra el registro de dispositivos para autenticación
/// biométrica (huella digital, FaceID) en la app móvil.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/biometrics")]
public sealed class BiometricsController : ControllerBase
{
    private readonly ZorvianDbContext _db;
    private readonly IAuditLogRepository _auditLog;

    public BiometricsController(ZorvianDbContext db, IAuditLogRepository auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Registra un dispositivo para autenticación biométrica.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterBiometricRequest request)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "User not authenticated" });

        var existing = await _db.BiometricRegistrations
            .FirstOrDefaultAsync(b => b.UserId == userId && b.DeviceId == request.DeviceId);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.DeviceName = request.DeviceName;
        }
        else
        {
            _db.BiometricRegistrations.Add(new BiometricRegistration
            {
                UserId = userId,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
            });
        }

        await _db.SaveChangesAsync();

        await _auditLog.AddAsync(new AuditLog
        {
            EntityName = "BiometricRegistration",
            Action = "Register",
            EntityId = userId.ToString(),
            PerformedBy = userId,
        });

        return Ok(new { registered = true });
    }

    /// <summary>
    /// Verifica un intento de autenticación biométrica (registra en auditoría).
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyBiometricRequest request)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "User not authenticated" });

        var registration = await _db.BiometricRegistrations
            .FirstOrDefaultAsync(b => b.UserId == userId && b.DeviceId == request.DeviceId && b.IsActive);

        if (registration is null)
            return NotFound(new { error = "Dispositivo no registrado o inactivo" });

        registration.LastVerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.AddAsync(new AuditLog
        {
            EntityName = "BiometricRegistration",
            Action = "Verify",
            EntityId = userId.ToString(),
            PerformedBy = userId,
        });

        return Ok(new { verified = true, lastVerifiedAt = registration.LastVerifiedAt });
    }

    /// <summary>
    /// Obtiene la lista de dispositivos biométricos registrados del usuario actual.
    /// </summary>
    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "User not authenticated" });

        var devices = await _db.BiometricRegistrations
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BiometricDeviceResponse(
                b.Id, b.DeviceId, b.DeviceName, b.IsActive, b.LastVerifiedAt, b.CreatedAt))
            .ToListAsync();

        return Ok(devices);
    }

    /// <summary>
    /// Desactiva un dispositivo biométrico.
    /// </summary>
    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> Unregister(string deviceId)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { error = "User not authenticated" });

        var registration = await _db.BiometricRegistrations
            .FirstOrDefaultAsync(b => b.UserId == userId && b.DeviceId == deviceId);

        if (registration is null)
            return NotFound(new { error = "Dispositivo no encontrado" });

        _db.BiometricRegistrations.Remove(registration);
        await _db.SaveChangesAsync();

        await _auditLog.AddAsync(new AuditLog
        {
            EntityName = "BiometricRegistration",
            Action = "Unregister",
            EntityId = userId.ToString(),
            PerformedBy = userId,
        });

        return Ok(new { unregistered = true });
    }
}
