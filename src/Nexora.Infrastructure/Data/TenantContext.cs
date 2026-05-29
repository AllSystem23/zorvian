using Nexora.Core.Interfaces;

namespace Nexora.Infrastructure.Data;

public sealed class TenantContext : ITenantContext
{
    public string TenantId { get; private set; } = string.Empty;

    public void SetTenant(string tenantId)
    {
        TenantId = tenantId;
    }
}
