using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de autenticación. Maneja inicio de sesión, renovación de tokens, cierre de sesión y verificación de salud del servicio.
/// </summary>
[ApiController]
[Route("zorvian/v1/auth")]
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
    /// Inicia sesión con correo y contraseña (sin Firebase JS SDK). Válido para clientes web.
    /// </summary>
    [HttpPost("login-password")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
    {
        var result = await _authService.LoginWithPasswordAsync(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid credentials" });

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
    /// Obtiene la información del usuario autenticado actualmente.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _authService.GetMeAsync(userId);
        if (result is null)
            return NotFound();

        return Ok(result);
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
