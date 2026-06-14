using Zorvian.Core.Attributes;

namespace Zorvian.Core.Entities;

public sealed class Client : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    [Encrypted]
    public string? IdentificationNumber { get; set; }

    [Encrypted]
    public string? Phone { get; set; }

    [Encrypted]
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? References { get; set; }
    public string Status { get; set; } = "active";
    public decimal? CreditLimit { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<Quote> Quotes { get; set; } = [];
    public ICollection<Credit> Credits { get; set; } = [];
    public ICollection<Warranty> Warranties { get; set; } = [];
}
