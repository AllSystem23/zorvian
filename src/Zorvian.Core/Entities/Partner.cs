namespace Zorvian.Core.Entities;

public sealed class Partner : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string PartnerType { get; set; } = "implementer";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? City { get; set; }
    public string Status { get; set; } = "active";
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? CommissionRate { get; set; }
    public string? ContractUrl { get; set; }
    public int ClientsReferred { get; set; }
    public decimal RevenueGenerated { get; set; }
    public DateTime? CertifiedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public string? Notes { get; set; }
}
