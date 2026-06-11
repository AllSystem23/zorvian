using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class PaymentMilestone : BaseEntity
{
    public Guid ServiceContractId { get; set; }
    public ServiceContract ServiceContract { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string? DeliverableDescription { get; set; }
    public DateOnly EstimatedDate { get; set; }
    public DateOnly? CompletionDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? DeliverableFileUrl { get; set; }
    public string? ApprovalNotes { get; set; }
}
