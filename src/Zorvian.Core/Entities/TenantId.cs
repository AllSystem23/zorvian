namespace Zorvian.Core.Entities;

public sealed record TenantId(Guid Value)
{
    public override string ToString() => Value.ToString();
    
    public static TenantId FromString(string value) => new(Guid.Parse(value));
    public static TenantId FromGuid(Guid value) => new(value);

    public static implicit operator string(TenantId tenantId) => tenantId.Value.ToString();
    public static implicit operator TenantId(string value) => new(Guid.Parse(value));
}
