namespace Zorvian.Core.Entities;

public sealed class VacationPaymentReceipt : BaseEntity
{
    public Guid VacationRequestId { get; set; }
    public VacationRequest VacationRequest { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    // Sequential number (e.g. "RV-20261013-0001")
    public string ReceiptNumber { get; set; } = string.Empty;

    // Vacation period
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public decimal BusinessDays { get; set; }

    // Salary info at payment time
    public decimal DailySalary { get; set; }
    public decimal MonthlySalary { get; set; }

    // Concepts
    public decimal VacationPay { get; set; }
    public decimal InssEmployeeAmount { get; set; }
    public decimal InssEmployeeRate { get; set; }

    // Totals
    public decimal TotalAssignments => VacationPay;
    public decimal TotalDeductions => InssEmployeeAmount;
    public decimal NetPay => VacationPay - InssEmployeeAmount;

    // Multi-country
    public string CountryCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "NIO";

    // Metadata
    public DateOnly PaymentDate { get; set; }
    public string? Observations { get; set; }
    public string? PaidBy { get; set; }
    public string Status { get; set; } = "paid";
}
