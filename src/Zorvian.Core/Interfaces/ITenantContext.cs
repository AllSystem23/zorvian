namespace Zorvian.Core.Interfaces;

public interface ITenantContext
{
    string TenantId { get; }
    Guid? CurrentUserId { get; }
    Guid? CurrentEmployeeId { get; }
    void SetTenantId(string tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
}
