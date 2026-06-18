using System.Threading;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class TenantContext : ITenantContext, ITenantContextWriter
{
    private static readonly AsyncLocal<TenantId?> TenantIdValue = new();
    private static readonly AsyncLocal<bool> IsSuperAdminValue = new();
    private static readonly AsyncLocal<Guid?> CurrentUserIdValue = new();
    private static readonly AsyncLocal<Guid?> CurrentEmployeeIdValue = new();

    public TenantId TenantId => TenantIdValue.Value ?? new(Guid.Empty);
    public bool IsSuperAdmin => IsSuperAdminValue.Value;
    public Guid? CurrentUserId => CurrentUserIdValue.Value;
    public Guid? CurrentEmployeeId => CurrentEmployeeIdValue.Value;

    public void SetTenantId(TenantId tenantId)
    {
        TenantIdValue.Value = tenantId;
    }

    public void SetCurrentUser(Guid? userId, Guid? employeeId)
    {
        CurrentUserIdValue.Value = userId;
        CurrentEmployeeIdValue.Value = employeeId;
    }

    public void SetIsSuperAdmin(bool isSuperAdmin)
    {
        IsSuperAdminValue.Value = isSuperAdmin;
    }
}
