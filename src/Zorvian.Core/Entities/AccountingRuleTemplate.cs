namespace Zorvian.Core.Entities;

public sealed class AccountingRuleTemplate : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty;
    public string ProcessTrigger { get; set; } = string.Empty; // e.g., 'SALE_INVOICE'
    public string EntryStructureJson { get; set; } = "{}"; // JSON map for debits/credits
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }
}
