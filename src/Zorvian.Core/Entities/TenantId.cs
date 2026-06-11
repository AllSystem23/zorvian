namespace Zorvian.Core.Entities;

public sealed record TenantId
{
    public Guid Value { get; }
    private readonly string _original;

    public TenantId(Guid value)
    {
        Value = value;
        _original = value.ToString();
    }

    private TenantId(Guid value, string original)
    {
        Value = value;
        _original = original;
    }

    public override string ToString() => _original;

    public static TenantId FromString(string value)
    {
        if (Guid.TryParse(value, out var guid))
            return new TenantId(guid, value);
        return new TenantId(Guid.Empty, value);
    }

    public static TenantId FromGuid(Guid value) => new(value);

    public static implicit operator string(TenantId tenantId) => tenantId._original;
    public static implicit operator TenantId(string value) => FromString(value);

    public bool TryGetCompanyId(out Guid companyId)
    {
        return Guid.TryParse(_original, out companyId);
    }
}
