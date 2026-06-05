namespace Zorvian.Application.DTOs.Auth;

public sealed record LoginRequest(string IdToken, string? DeviceFingerprint = null);
