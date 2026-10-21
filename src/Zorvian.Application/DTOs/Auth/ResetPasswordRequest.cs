namespace Zorvian.Application.DTOs.Auth;

/// <summary>
/// Body de <c>POST zorvian/v1/auth/reset-password</c>.
/// </summary>
public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);
