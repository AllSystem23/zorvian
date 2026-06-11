namespace Zorvian.Core.Entities;

public sealed class ServiceProvider : BaseEntity
{
    public Guid CollaboratorId { get; set; }
    public Collaborator Collaborator { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string BusinessName { get; set; } = string.Empty;
    public string? FiscalAddress { get; set; }
    public string? TaxRegime { get; set; }
    public string? ProfessionalLicense { get; set; }
    public string? Specialization { get; set; }
    public string ServiceCategory { get; set; } = string.Empty;
    public string? InsurancePolicy { get; set; }
    public DateOnly? InsuranceExpiration { get; set; }
    public string Status { get; set; } = "active";

    public ICollection<ServiceContract> Contracts { get; set; } = [];
}
