namespace Zorvian.Core.Entities;

public sealed class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string UnitOfMeasure { get; set; } = "unit";
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? TaxCategoryId { get; set; }
    public TaxCategory? TaxCategory { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
}
