namespace Zorvian.Core.Entities;

public sealed class Warranty : BaseEntity
{
    public string WarrantyNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DurationMonths { get; set; }
    public string? Terms { get; set; }
    public string Status { get; set; } = "active";
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<WarrantyClaim> Claims { get; set; } = [];
}
