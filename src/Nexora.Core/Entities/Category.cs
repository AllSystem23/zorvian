namespace Nexora.Core.Entities;

public sealed class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
