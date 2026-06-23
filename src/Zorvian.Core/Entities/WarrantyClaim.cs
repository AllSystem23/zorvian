using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

public sealed class WarrantyClaim : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public DateOnly ClaimDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public WarrantyStatus Status { get; set; } = WarrantyStatus.Registered;
    public string? Resolution { get; set; }
    public string? ResolutionType { get; set; }
    public DateOnly? ResolutionDate { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedBy { get; set; }
    public Guid BranchId { get; set; }

    public Guid? WorkshopId { get; set; }
    public ServiceWorkshop? Workshop { get; set; }
    public Guid? TechnicianId { get; set; }
    public WorkshopTechnician? Technician { get; set; }
    public Guid? ProviderId { get; set; }
    public WarrantyProvider? Provider { get; set; }
    public string? Accessories { get; set; }
    public string? FailureType { get; set; }
    public string? FailureDescription { get; set; }
    public string Priority { get; set; } = "medium";
    public string? ProductCondition { get; set; }
    public DateTime? WorkshopAssignedAt { get; set; }
    public DateTime? SlaDeadline { get; set; }
    public DateTime? SlaBreachedAt { get; set; }
    public DateTime? ProviderReferredAt { get; set; }
    public string? ProviderAuthorizationCode { get; set; }
}
