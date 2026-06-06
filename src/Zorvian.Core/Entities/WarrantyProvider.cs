namespace Zorvian.Core.Entities;

public sealed class WarrantyProvider : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string Type { get; set; } = "manufacturer";
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public int AvgResponseHours { get; set; } = 96;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }

    public ICollection<ProviderContact> Contacts { get; set; } = [];
}
