namespace Zorvian.Application.DTOs.Auth;

public sealed record UpdateDisplayNameRequest(string DisplayName);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public sealed record ChangePasswordResponse(bool Success, string? Error);

public sealed record RequestEmailChangeRequest(string NewEmail);

public sealed record ConfirmEmailChangeRequest(string NewEmail, string VerificationCode);
