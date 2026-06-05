using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class TenantContext : ITenantContext, ITenantContextWriter
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid? CurrentUserId { get; private set; }
    public Guid? CurrentEmployeeId { get; private set; }

    public void SetTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or whitespace", nameof(tenantId));
        TenantId = tenantId;
    }

    public void SetCurrentUser(Guid? userId, Guid? employeeId)
    {
        CurrentUserId = userId;
        CurrentEmployeeId = employeeId;
    }
}
