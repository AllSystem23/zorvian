namespace Zorvian.Application.DTOs.ML;

public sealed class AccountingAnomalyDto
{
    public Guid AccountingEntryId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Severity { get; set; } = "low";
}

public sealed class AccountingAnomalyReportDto
{
    public List<AccountingAnomalyDto> Anomalies { get; set; } = [];
    public int TotalEntriesAnalyzed { get; set; }
    public int AnomalyCount { get; set; }
}
