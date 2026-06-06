using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class TenantContext : ITenantContext, ITenantContextWriter
{
    public TenantId TenantId { get; private set; } = new(Guid.Empty);
    public bool IsSuperAdmin { get; private set; }
    public Guid? CurrentUserId { get; private set; }
    public Guid? CurrentEmployeeId { get; private set; }

    public void SetTenantId(TenantId tenantId)
    {
        TenantId = tenantId;
    }

    public void SetCurrentUser(Guid? userId, Guid? employeeId)
    {
        CurrentUserId = userId;
        CurrentEmployeeId = employeeId;
    }

    public void SetIsSuperAdmin(bool isSuperAdmin)
    {
        IsSuperAdmin = isSuperAdmin;
    }
}
