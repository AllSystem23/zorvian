using System;
using System.ComponentModel.DataAnnotations;

namespace Zorvian.Core.Entities
{
    public class CheckAuditTrail : BaseEntity
    {
        [Required]
        public Guid CheckId { get; set; }
        public Check? Check { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
