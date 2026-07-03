using Zorvian.Core.Entities;

namespace Zorvian.Core.Interfaces;

public interface ITenantContext
{
    TenantId TenantId { get; }
    Guid? EffectiveCompanyId => IsSuperAdmin ? null : TenantId.TryGetCompanyId(out var id) ? id : null;
    bool IsSuperAdmin { get; }
    Guid? CurrentUserId { get; }
    Guid? CurrentEmployeeId { get; }
    string? GetUserIdentifier() => CurrentUserId?.ToString();
}

public interface ITenantContextWriter
{
    void SetTenantId(TenantId tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
    void SetIsSuperAdmin(bool isSuperAdmin);
}
