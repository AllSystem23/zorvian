using System;
using System.ComponentModel.DataAnnotations;

namespace Zorvian.Core.Entities
{
    public class Checkbook : BaseEntity
    {
        [Required]
        public Guid BankAccountId { get; set; }
        public BankAccount? BankAccount { get; set; }

        [Required]
        [StringLength(20)]
        public string Series { get; set; } = string.Empty;

        [Required]
        public long StartNumber { get; set; }

        [Required]
        public long EndNumber { get; set; }

        public long NextNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
