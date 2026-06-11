using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class ServiceContract : BaseEntity
{
    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; } = null!;

    public string ContractNumber { get; set; } = string.Empty;
    public string ContractName { get; set; } = string.Empty;
    public string? Scope { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalContractAmount { get; set; }

    public string Currency { get; set; } = "NIO";
    public string? PaymentTerms { get; set; }
    public string? PaymentMilestonesJson { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "draft";
    public string? ContractFileUrl { get; set; }
    public string? Notes { get; set; }

    public ICollection<PaymentMilestone> Milestones { get; set; } = [];
}
