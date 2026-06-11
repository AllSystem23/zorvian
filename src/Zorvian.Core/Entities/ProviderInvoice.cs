using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class ProviderInvoice : BaseEntity
{
    public Guid PaymentMilestoneId { get; set; }
    public PaymentMilestone PaymentMilestone { get; set; } = null!;

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InvoiceAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WithholdingAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    public string Currency { get; set; } = "NIO";

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExchangeRate { get; set; } = 1.0m;

    public string? InvoiceFileUrl { get; set; }
    public string Status { get; set; } = "received";
    public DateOnly? PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }

    public Guid? AccountingEntryId { get; set; }
}
