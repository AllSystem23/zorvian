namespace Zorvian.Application.DTOs.Mfa;

public sealed record VerifyMfaRequest(string Code);

public sealed record EnableMfaRequest(string Code);

public sealed record DisableMfaRequest(string Password, string Code);

public sealed record MfaLoginRequest(string MfaToken, string Code, string? DeviceFingerprint = null);
