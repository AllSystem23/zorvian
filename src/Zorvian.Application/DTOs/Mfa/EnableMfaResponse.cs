namespace Zorvian.Application.DTOs.Mfa;

public sealed record EnableMfaResponse(string SecretKey, string QrCodeUri);
