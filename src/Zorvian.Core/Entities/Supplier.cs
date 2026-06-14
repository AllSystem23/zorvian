using Zorvian.Core.Attributes;

namespace Zorvian.Core.Entities;

public sealed class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }

    [Encrypted]
    public string? Phone { get; set; }
    public string? Email { get; set; }

    [Encrypted]
    public string? Address { get; set; }

    [Encrypted]
    public string? TaxId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
