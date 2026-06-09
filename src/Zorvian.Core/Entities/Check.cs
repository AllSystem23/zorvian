using System;
using System.ComponentModel.DataAnnotations;

namespace Zorvian.Core.Entities
{
    public enum CheckStatus
    {
        Draft,
        PendingApproval,
        Approved,
        Printed,
        Delivered,
        Cashed,
        Cancelled
    }

    public class Check : BaseEntity
    {
        [Required]
        public Guid BankAccountId { get; set; }
        public BankAccount? BankAccount { get; set; }

        [Required]
        public long CheckNumber { get; set; }

        [Required]
        public DateTime IssueDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Beneficiary { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = "USD";

        [StringLength(500)]
        public string? Description { get; set; }

        public CheckStatus Status { get; set; } = CheckStatus.Draft;
    }
}
