namespace Zorvian.Core.Entities;

public sealed class IntercompanyTransaction : BaseEntity
{
    public Guid FromCompanyId { get; set; }
    public Guid ToCompanyId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NIO";
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Completed, Rejected
    
    public Company FromCompany { get; set; } = null!;
    public Company ToCompany { get; set; } = null!;
}
