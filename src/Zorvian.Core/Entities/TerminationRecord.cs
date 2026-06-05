namespace Zorvian.Core.Entities;

public enum TerminationReason
{
    VoluntaryResignation,
    JustifiedDismissal,
    UnjustifiedDismissal,
    MutualAgreement
}

public sealed class TerminationRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly TerminationDate { get; set; }
    public TerminationReason Reason { get; set; }
    
    // Settlement Breakdown
    public decimal GrossSettlement { get; set; }
    public decimal NetSettlement { get; set; }
    public decimal SeveranceDays { get; set; }
    public decimal AccruedVacationPay { get; set; }
    public decimal AccruedAguinaldoPay { get; set; }
    
    public string? SignedDocumentUrl { get; set; }
    public string Status { get; set; } = "draft"; // draft, approved, processed
}
