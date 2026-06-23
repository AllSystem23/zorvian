using Zorvian.Core.Enums;

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
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DurationMonths { get; set; }
    public string? Terms { get; set; }
    public string? SerialNumber { get; set; }
    public string? Imei { get; set; }
    public string? LotNumber { get; set; }
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Registered;
    public Guid BranchId { get; set; }

    public int? SlaHours { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public DateTime? SlaBreachedAt { get; set; }

    public ICollection<WarrantyClaim> Claims { get; set; } = [];
    public ICollection<WarrantyCost> Costs { get; set; } = [];
}
