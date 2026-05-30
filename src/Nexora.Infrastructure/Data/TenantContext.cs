using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Data;

public sealed class TenantContext : ITenantContext
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid? CurrentUserId { get; private set; }
    public Guid? CurrentEmployeeId { get; private set; }

    public void SetTenant(string tenantId)
    {
        TenantId = tenantId;
    }

    public void SetCurrentUser(Guid? userId, Guid? employeeId)
    {
        CurrentUserId = userId;
        CurrentEmployeeId = employeeId;
    }
}
