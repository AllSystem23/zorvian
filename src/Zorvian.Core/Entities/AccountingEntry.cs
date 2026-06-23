namespace Zorvian.Core.Entities;

public sealed class AccountingEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string Status { get; set; } = "draft";
    public Guid AccountingPeriodId { get; set; }
    public AccountingPeriod AccountingPeriod { get; set; } = null!;
    public Guid? BranchId { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public string CurrencyCode { get; set; } = "NIO";
    public decimal? ExchangeRateToReporting { get; set; }

    public ICollection<AccountingEntryDetail> Details { get; set; } = [];
}

public sealed class AccountingEntryDetail : BaseEntity
{
    public Guid AccountingEntryId { get; set; }
    public AccountingEntry AccountingEntry { get; set; } = null!;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
}
