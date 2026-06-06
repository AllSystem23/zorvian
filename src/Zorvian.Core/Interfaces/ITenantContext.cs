using Zorvian.Core.Entities;

namespace Zorvian.Core.Interfaces;

public interface ITenantContext
{
    TenantId TenantId { get; }
    bool IsSuperAdmin { get; }
    Guid? CurrentUserId { get; }
    Guid? CurrentEmployeeId { get; }
}

public interface ITenantContextWriter
{
    void SetTenantId(TenantId tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
    void SetIsSuperAdmin(bool isSuperAdmin);
}
