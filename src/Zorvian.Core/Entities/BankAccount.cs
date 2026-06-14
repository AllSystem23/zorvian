using System;
using System.ComponentModel.DataAnnotations;
using Zorvian.Core.Attributes;

namespace Zorvian.Core.Entities
{
    public class BankAccount : BaseEntity
    {
        [Required]
        public Guid BankId { get; set; }
        public Bank? Bank { get; set; }

        [Required]
        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required]
        [StringLength(255)] // Aumentado para soportar base64 de encriptación
        [Encrypted]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = "USD";

        public decimal CurrentBalance { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
