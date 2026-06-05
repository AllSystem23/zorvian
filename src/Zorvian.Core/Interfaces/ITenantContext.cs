namespace Zorvian.Core.Interfaces;

public interface ITenantContext
{
    string TenantId { get; }
    Guid? CurrentUserId { get; }
    Guid? CurrentEmployeeId { get; }
}

public interface ITenantContextWriter
{
    void SetTenantId(string tenantId);
    void SetCurrentUser(Guid? userId, Guid? employeeId);
}
