namespace Zorvian.Core.Entities;

public sealed class ServiceWorkshop : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int AvgResponseHours { get; set; } = 48;
    public int AvgRepairHours { get; set; } = 72;
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<WorkshopTechnician> Technicians { get; set; } = [];
}
