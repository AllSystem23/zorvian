namespace Zorvian.Core.Entities;

public enum TerminationReason
{
    VoluntaryResignation,
    JustifiedDismissal,
    UnjustifiedDismissal,
    MutualAgreement
}

public sealed class TerminationRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly TerminationDate { get; set; }
    public TerminationReason Reason { get; set; }

    // Service period
    public DateOnly HireDate { get; set; }
    public int DaysWorked { get; set; }
    public decimal MonthsWorked { get; set; }
    public decimal YearsWorked { get; set; }
    public decimal DaysAccrued25PerMonth { get; set; } // 2.5 días/mes = 30/12

    // Salary info at termination
    public decimal MonthlySalary { get; set; }
    public decimal DailySalary { get; set; }

    // ── Aguinaldo (Art. 93 CT) ──
    // Proporcional: (daysWorked / 365) * monthlySalary
    public decimal AguinaldoPay { get; set; }

    // ── Vacaciones (Art. 78 CT) ──
    // Days accrued (2.5 per month), days taken, days to pay
    public decimal VacationDaysAccrued { get; set; }
    public decimal VacationDaysTaken { get; set; }
    public decimal VacationDaysToPay { get; set; }
    public decimal VacationPay { get; set; } // (daysAccrued - daysTaken) * dailySalary

    // ── Indemnización por Antigüedad (Art. 45 CT) ──
    // Tiered formula or proportional for resignation
    public decimal SeveranceDays { get; set; }
    public decimal SeverancePay { get; set; }

    // ── Indemnización por Cargo de Confianza (Art. 46-47 CT) ──
    // Additional severance for trust positions (CSJ/MITRAB), capped at C$100,000
    public bool IsTrustPosition { get; set; }
    public decimal TrustPositionPay { get; set; }

    // ── Salario Pendiente ──
    // Days worked in current pay period not yet paid
    public int PendingSalaryDays { get; set; }
    public decimal PendingSalaryPay { get; set; }

    // ── Horas Extras ──
    // Overtime hours and pay (manual input or calculated)
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; } // hours * hourlyRate

    // ── INSS (employee portion) ──
    // 7% on Salario Ordinario only (prestaciones are EXEMPT per Decreto 06-2019)
    public decimal InssLaboralAmount { get; set; }

    // ── IR (Impuesto sobre la Renta) ──
    // On Salario Ordinario only (prestaciones are EXEMPT per Art. 19.2 Ley 822)
    public decimal IrSalaryAmount { get; set; } // IR on pending salary
    public decimal IrTotalAmount { get; set; } // Total IR (sum of all IR components)

    // ── Employer costs (informational, not deducted from employee) ──
    public decimal InssPatronalAmount { get; set; } // 21.5% employer INSS
    public decimal InatecAmount { get; set; } // 2% INATEC

    // ── Totals ──
    public decimal GrossSettlement { get; set; } // All income concepts
    public decimal TotalDeductions { get; set; } // INSS + IR + loans + advances
    public decimal NetSettlement => GrossSettlement - TotalDeductions; // Always computed

    // ── Multi-country info ──
    // Country-specific data for frontend rendering (currency, rates, legal refs)
    public string CountryCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "NIO";
    public string CountryName { get; set; } = string.Empty;
    public decimal InssEmployeeRateDisplay { get; set; } // For display: 7%, 5%, etc.
    public decimal InssEmployerRateDisplay { get; set; } // For display: 21.5%, etc.
    public decimal OtherEmployerRateDisplay { get; set; } // For display: 2% INATEC, etc.
    public string OtherEmployerName { get; set; } = string.Empty; // "INATEC", "IFAM", etc.

    public string? SignedDocumentUrl { get; set; }
    public string Status { get; set; } = "draft"; // draft, approved, processed
}
