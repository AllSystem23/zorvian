namespace Zorvian.Core.Entities;

public sealed class Employee : BaseEntity
{
    public string? EmployeeCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? IdentificationType { get; set; }
    public string? IdentificationNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Position { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public decimal? Salary { get; set; }
    public string? SalaryType { get; set; } = "monthly";
    public string Status { get; set; } = "active";
    public string? PhotoUrl { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountType { get; set; }
    public Guid? UserId { get; set; }

    public ICollection<EmployeeBankAccount> BankAccounts { get; set; } = [];
    public ICollection<EmployeeSupervisor> SupervisedBy { get; set; } = [];
    public ICollection<EmployeeSupervisor> Supervisors { get; set; } = [];
    public ICollection<EmployeeDocument> Documents { get; set; } = [];
    public ICollection<LeaveBalances> LeaveBalances { get; set; } = [];
    public ICollection<EmployeeHistory> History { get; set; } = [];
    public ICollection<VacationRequest> VacationRequests { get; set; } = [];
    public ICollection<PermissionRequest> PermissionRequests { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<Credit> ManagedCredits { get; set; } = [];
    public ICollection<CashRegister> CashRegisters { get; set; } = [];
    public ICollection<InventoryMovement> PerformedInventoryMovements { get; set; } = [];
}
