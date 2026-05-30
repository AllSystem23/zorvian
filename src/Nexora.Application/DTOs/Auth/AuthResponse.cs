namespace Nexora.Application.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserInfo User
);

public sealed record UserInfo(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    string TenantId,
    string? EmployeeId = null
);
