using System;

namespace Zorvian.Core.Entities;

public sealed class WarrantyRevenueSchedule : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    
    public decimal Amount { get; set; }
    public DateOnly RecognitionDate { get; set; }
    public bool IsRecognized { get; set; }
    
    public Guid? AccountingEntryId { get; set; }
}
