using System;
using System.ComponentModel.DataAnnotations;

namespace Zorvian.Core.Entities
{
    public class CheckPrintTemplate : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid BankId { get; set; }
        public Bank? Bank { get; set; }

        [Required]
        public string ConfigurationJson { get; set; } = string.Empty; // Holds X, Y, W, H for fields
    }
}
