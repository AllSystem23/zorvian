namespace Zorvian.Core.Entities;

public sealed class CompanySettings : BaseEntity
{
    public int VacationDaysPerYear { get; set; } = 15;
    public string VacationAccrualMethod { get; set; } = "monthly";
    public int LateToleranceMinutes { get; set; } = 15;
    public decimal WorkingHoursPerDay { get; set; } = 8;
    public string WorkingDays { get; set; } = "MON,TUE,WED,THU,FRI";
    public bool OvertimeEnabled { get; set; }
    public string? Timezone { get; set; } = "America/Managua";
    public string Currency { get; set; } = "NIO";
    public string? DateFormat { get; set; } = "DD/MM/YYYY";
    public string? ApprovalFlowConfig { get; set; } // JSON: [{"step":1,"role":"Supervisor"},{"step":2,"role":"Rrhh"}]

    // Cobranza + Mora
    public decimal LateFeeDailyRate { get; set; } = 0.001m;
    public decimal LateFeePercentage { get; set; } = 0.05m;
    public int LateFeeGracePeriod { get; set; } = 0;

    // Facturación / IVA
    public bool TaxEnabled { get; set; } = true;
    public decimal TaxRate { get; set; } = 0.15m;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
