using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

public sealed class Lead : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? City { get; set; }
    public string CountryCode { get; set; } = "NI";
    public string? Source { get; set; } // Web, WhatsApp, FB, Referral, etc.
    public string? InterestLevel { get; set; } // Low, Medium, High
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public Guid? AssignedToId { get; set; }
    public Employee? AssignedTo { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    public ICollection<CommercialActivity> Activities { get; set; } = [];
}
