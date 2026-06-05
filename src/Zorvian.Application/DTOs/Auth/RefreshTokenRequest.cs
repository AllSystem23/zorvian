namespace Zorvian.Application.DTOs.Auth;

public sealed record RefreshTokenRequest(string RefreshToken, string? DeviceFingerprint = null);
