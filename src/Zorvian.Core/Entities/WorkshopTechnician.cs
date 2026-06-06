namespace Zorvian.Core.Entities;

public sealed class WorkshopTechnician : BaseEntity
{
    public Guid WorkshopId { get; set; }
    public ServiceWorkshop Workshop { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string? Identification { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string[] Specialties { get; set; } = [];
    public bool IsCertified { get; set; }
    public DateOnly? CertificationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? AvgRepairMinutes { get; set; }
}
