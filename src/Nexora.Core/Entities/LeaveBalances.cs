namespace Nexora.Core.Entities;

public sealed class LeaveBalances : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public int Year { get; set; }
    public decimal VacationDaysAccrued { get; set; }
    public decimal VacationDaysTaken { get; set; }
    public decimal VacationDaysPending { get; set; }
    public decimal SickDaysAccrued { get; set; }
    public decimal SickDaysTaken { get; set; }
    public decimal PersonalDaysAccrued { get; set; }
    public decimal PersonalDaysTaken { get; set; }

    public decimal VacationDaysRemaining => VacationDaysAccrued - VacationDaysTaken - VacationDaysPending;
    public decimal SickDaysRemaining => SickDaysAccrued - SickDaysTaken;
    public decimal PersonalDaysRemaining => PersonalDaysAccrued - PersonalDaysTaken;
}
