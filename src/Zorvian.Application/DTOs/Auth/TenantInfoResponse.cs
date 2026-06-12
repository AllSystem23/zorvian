namespace Zorvian.Application.DTOs.Auth;

public sealed record TenantInfoResponse(
    string TenantId,
    string CompanyName,
    bool IsCurrent
);
