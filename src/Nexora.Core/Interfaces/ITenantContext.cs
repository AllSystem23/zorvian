namespace Nexora.Core.Interfaces;

public interface ITenantContext
{
    string TenantId { get; }
    Guid? CurrentUserId { get; }
    Guid? CurrentEmployeeId { get; }
    void SetTenant(string tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
}
