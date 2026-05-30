using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Auth;
using Nexora.Application.Services;

namespace Nexora.Web.Controllers;

/// <summary>
/// Controlador de autenticación. Maneja inicio de sesión, renovación de tokens, cierre de sesión y verificación de salud del servicio.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Inicia sesión con un token de Firebase y devuelve tokens JWT de acceso y renovación.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid Firebase token" });

        return Ok(new LoginResponse(result));
    }

    /// <summary>
    /// Renueva el token de acceso usando un token de renovación válido.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid or expired refresh token" });

        return Ok(result);
    }

    /// <summary>
    /// Cierra la sesión invalidando el token de renovación proporcionado.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        if (!result)
            return NotFound(new { error = "Refresh token not found" });

        return NoContent();
    }

    /// <summary>
    /// Verifica que el servicio de autenticación esté operativo.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
