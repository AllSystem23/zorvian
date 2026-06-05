using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.DTOs.Mfa;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("zorvian/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IMfaService _mfaService;

    public AuthController(AuthService authService, IMfaService mfaService)
    {
        _authService = authService;
        _mfaService = mfaService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [Audit("Auth", "Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid Firebase token" });

        return Ok(new LoginResponse(result));
    }

    [HttpPost("login-password")]
    [AllowAnonymous]
    [Audit("Auth", "LoginPassword")]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginPasswordRequest request)
    {
        var result = await _authService.LoginWithPasswordStep1Async(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid credentials" });

        if (result.MfaRequiredResponse is not null)
            return Ok(new { mfa_required = true, mfa_token = result.MfaRequiredResponse.MfaToken });

        return Ok(new LoginResponse(result.AuthResponse!));
    }

    [HttpPost("mfa/login")]
    [AllowAnonymous]
    public async Task<IActionResult> CompleteMfaLogin([FromBody] MfaLoginRequest request)
    {
        var result = await _authService.CompleteMfaLoginAsync(request.MfaToken, request.Code, request.DeviceFingerprint);
        if (result is null)
            return Unauthorized(new { error = "Invalid MFA token or code" });

        return Ok(new LoginResponse(result));
    }

    [HttpPost("mfa/generate")]
    [Authorize]
    public async Task<IActionResult> GenerateMfaSecret()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _mfaService.GenerateSecretAsync(userId.Value);
        return Ok(result);
    }

    [HttpPost("mfa/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyAndEnableMfa([FromBody] EnableMfaRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var success = await _mfaService.VerifyAndEnableAsync(userId.Value, request.Code);
        if (!success)
            return BadRequest(new { error = "Invalid code. Ensure you scanned the QR code and entered the correct code." });

        return Ok(new { message = "MFA enabled successfully" });
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var success = await _mfaService.DisableAsync(userId.Value, request.Password, request.Code);
        if (!success)
            return BadRequest(new { error = "Invalid password or MFA code" });

        return Ok(new { message = "MFA disabled successfully" });
    }

    [HttpGet("mfa/status")]
    [Authorize]
    public async Task<IActionResult> GetMfaStatus()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var user = await _authService.GetMeAsync(userId.Value);
        return Ok(new { mfa_enabled = user is not null });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (result is null)
            return Unauthorized(new { error = "Invalid or expired refresh token" });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        if (!result)
            return NotFound(new { error = "Refresh token not found" });

        return NoContent();
    }

    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        await _authService.RevokeAllSessionsAsync(userId.Value);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var result = await _authService.GetMeAsync(userId.Value);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return null;
        return userId;
    }
}
