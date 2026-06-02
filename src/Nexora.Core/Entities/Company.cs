namespace Nexora.Core.Entities;

public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Country { get; set; } = "Nicaragua";
    public string Currency { get; set; } = "NIO";
    public string Timezone { get; set; } = "America/Managua";
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string SubscriptionPlan { get; set; } = "starter";
    public string SubscriptionStatus { get; set; } = "active";
    public int MaxEmployees { get; set; } = 50;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
    public CompanySettings? Settings { get; set; }
    public ICollection<Branch> Branches { get; set; } = [];
}
