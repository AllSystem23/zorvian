using System;
using System.ComponentModel.DataAnnotations;

namespace Zorvian.Core.Entities
{
    public class Bank : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? SwiftCode { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
