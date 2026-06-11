namespace Zorvian.Core.Entities;

public sealed class Collaborator : BaseEntity
{
    public string CollaboratorCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CollaboratorType { get; set; } = "employee"; // Matches CollaboratorType enum values
    
    public string? TaxId { get; set; } // RUC/NIT/Cédula
    public string? Nationality { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    
    public string Status { get; set; } = "active";
    public string? PhotoUrl { get; set; }
    
    // Bank info common to all
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountType { get; set; }

    public Guid? UserId { get; set; }
}
