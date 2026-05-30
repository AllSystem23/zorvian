namespace Nexora.Core.Entities;

public sealed class LeaveType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPaid { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool RequiresApproval { get; set; }
    public int? MaxDaysPerRequest { get; set; }
    public int? MaxDaysPerMonth { get; set; }
    public int? MaxDaysPerYear { get; set; }
    public string? Country { get; set; } = "NI";
    public Guid? CompanyId { get; set; } // null = global default, set = per-company override

    public Company? Company { get; set; }
    public ICollection<PermissionRequest> PermissionRequests { get; set; } = [];
}
