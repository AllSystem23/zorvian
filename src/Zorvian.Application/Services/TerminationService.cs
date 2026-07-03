using System.Text.Json;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class TerminationService
{
    private readonly ITerminationRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IBenefitProvisionRepository _benefitRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly IVacationRepository _vacationRepo;

    public TerminationService(
        ITerminationRepository repo,
        IEmployeeRepository employeeRepo,
        IBenefitProvisionRepository benefitRepo,
        ICountryTaxConfigRepository taxConfigRepo,
        IVacationRepository vacationRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _benefitRepo = benefitRepo;
        _taxConfigRepo = taxConfigRepo;
        _vacationRepo = vacationRepo;
    }

    /// <summary>
    /// Calculates the final termination settlement for an employee.
    /// </summary>
    /// <param name="paidThroughDate">If provided, employee is paid through this date (pending salary = 0).</param>
    public async Task<TerminationRecord?> CalculateAsync(Guid employeeId, TerminationReason reason, DateOnly terminationDate, DateOnly? paidThroughDate = null, decimal overtimeHours = 0, decimal overtimePay = 0)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found");

        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(employee.CountryCode);
        var monthlySalary = employee.Salary ?? 0;
        var dailySalary = monthlySalary / 30m;

        // ── Service period calculation ─────────────────────────────
        // Inclusive counting: both hire date and termination date are work days
        var daysWorked = terminationDate.DayNumber - employee.HireDate.DayNumber + 1;
        var monthsWorked = Math.Round((decimal)daysWorked / 30.42m, 2); // ~30.42 days/month avg
        var yearsWorked = Math.Round((decimal)daysWorked / 365m, 2);

        // Accurate year count for severance tiers
        var yearsOfService = terminationDate.Year - employee.HireDate.Year;
        if (terminationDate < employee.HireDate.AddYears(yearsOfService))
            yearsOfService--;

        // 2.5 days vacation per month (30 days/year ÷ 12 = 2.5)
        // Calculate directly without intermediate rounding for maximum precision
        var vacationDaysPerYear = taxConfig?.VacationDaysPerYear ?? 30;
        var daysAccrued25 = Math.Round((decimal)daysWorked / 30.42m * (vacationDaysPerYear / 12m), 2);

        // ── Flags de configuración por empleado ──
        // "¿Al Trabajador se le deduce?" — INSS, IR, Aguinaldo
        // "¿Trabajadora del Hogar con Dormida Adentro?" — prestaciones 1.5× (Art. 145 CT)
        var deductInss = employee.DeductInss;
        var deductIr = employee.DeductIr;
        var deductAguinaldo = employee.DeductAguinaldo;
        var isDomesticWithBoard = employee.IsDomesticWorkerWithBoard;

        // Art. 145 CT: Trabajadora del hogar con dormida adentro → prestaciones con 1.5× del salario
        var prestacionesMultiplier = isDomesticWithBoard ? 1.5m : 1.0m;

        // ── 1. Aguinaldo proporcional (Art. 93 CT) ───────────────
        // Solo se calcula si DeductAguinaldo = true
        // NIC Excel formula: =SI(AÑO(F14)<AÑO(F15);FECHA(AÑO(F15)-1;12;1);F14)
        var aguinaldoPay = 0m;
        if (deductAguinaldo)
        {
            var aguinaldoStartMonth = taxConfig?.AguinaldoPeriodStartMonth ?? 12;
            var aguinaldoStartDay = taxConfig?.AguinaldoPeriodStartDay ?? 1;
            DateOnly aguinaldoStartDate;
            if (employee.HireDate.Year < terminationDate.Year)
            {
                aguinaldoStartDate = new DateOnly(terminationDate.Year - 1, aguinaldoStartMonth, aguinaldoStartDay);
            }
            else
            {
                aguinaldoStartDate = employee.HireDate;
            }
            var aguinaldoDaysWorked = terminationDate.DayNumber - aguinaldoStartDate.DayNumber + 1;
            var aguinaldoSalary = monthlySalary * prestacionesMultiplier;
            aguinaldoPay = Math.Round((decimal)aguinaldoDaysWorked / 365m * aguinaldoSalary, 2);
        }

        // ── 2. Vacaciones pendientes (Art. 78 CT) ────────────────
        var vacationDaysTaken = await GetVacationDaysTakenAsync(employeeId, terminationDate);
        var vacationDaysToPay = Math.Max(0, daysAccrued25 - vacationDaysTaken);
        var vacationDailySalary = dailySalary * prestacionesMultiplier;
        var vacationPay = Math.Round(vacationDaysToPay * vacationDailySalary, 2);

        // ── 3. Indemnización por Antigüedad (Art. 45 CT) ─────────
        var severanceDays = 0m;
        var severancePay = 0m;

        if (daysWorked >= 30)
        {
            severanceDays = CalculateSeveranceDays(yearsOfService, taxConfig);

            if (reason == TerminationReason.VoluntaryResignation && yearsOfService < 1)
            {
                // One-step calculation matching Excel formula (avoids intermediate rounding)
                // Then derive days from the amount
                severancePay = Math.Round((decimal)daysWorked / 365m * monthlySalary * prestacionesMultiplier, 2);
                severanceDays = vacationDailySalary > 0 ? Math.Round(severancePay / vacationDailySalary, 2) : 0;
            }
            else
            {
                severancePay = Math.Round(vacationDailySalary * severanceDays, 2);
            }
        }

        // ── 4. Salario Pendiente del mes actual ─────────────────
        // If paidThroughDate >= terminationDate, employee is paid up to date (pending = 0)
        var pendingSalaryDays = (paidThroughDate.HasValue && paidThroughDate.Value >= terminationDate)
            ? 0
            : Math.Min(terminationDate.Day, daysWorked);
        var pendingSalaryPay = Math.Round((decimal)pendingSalaryDays * dailySalary, 2);

        // ── 5. Indemnización por Cargo de Confianza (Art. 46-47 CT) ──
        var isTrustPosition = employee.IsTrustPosition;
        var trustPositionPay = 0m;
        if (isTrustPosition && daysWorked >= 30)
        {
            trustPositionPay = Math.Min(yearsOfService * monthlySalary, 100000m);
        }

        // ── 6. Deducciones: INSS + IR (solo sobre Salario Ordinario) ──
        // Base INSS = SUMAN SALARIOS (pendiente + horas extras) + vacaciones + otros conceptos
        // overtimeHours and overtimePay come from parameters (TODO: wire up overtime input form)
        var inssRate = taxConfig != null ? taxConfig.InssEmployeeRate : 0.07m;
        var inssMax = taxConfig?.InssEmployeeMax > 0 ? taxConfig.InssEmployeeMax : 0m;

        // INSS Laboral: solo si DeductInss = true
        var inssLaboral = 0m;
        if (deductInss)
        {
            var inssBase = pendingSalaryPay + vacationPay + overtimePay;
            inssLaboral = Math.Round(inssBase * inssRate, 2);
            if (inssMax > 0) inssLaboral = Math.Min(inssLaboral, inssMax);
        }

        // IR: solo si DeductIr = true
        var irPendingSalary = 0m;
        var irOnVacation = 0m;
        var irOnTrustPosition = 0m;
        if (deductIr)
        {
            // IR sobre Salario Ordinario (método de proyección anual, Art. 23 Ley 822)
            var irOnSalary = CalculateIrOnMonthlySalary(monthlySalary, taxConfig);
            irPendingSalary = Math.Round(irOnSalary * pendingSalaryDays / 30m, 2);

            // IR Incremental sobre Pagos Ocionales (Art. 19.2 Reglamento Ley 822)
            // Base = SUMAN todos los pagos ocasionales (vacaciones + horas extras + otros)
            var totalOccasionalPayments = vacationPay + overtimePay;
            var occasionalNetOfInss = totalOccasionalPayments - Math.Round(totalOccasionalPayments * inssRate, 2);
            irOnVacation = CalculateIncrementalIr(monthlySalary, occasionalNetOfInss, taxConfig);

            // IR sobre Indemnización por Cargo de Confianza (Retención Definitiva Art. 19.3 L822)
            if (trustPositionPay > 0)
            {
                var excessOver450k = Math.Max(0, trustPositionPay - 450000m);
                if (excessOver450k > 0)
                {
                    irOnTrustPosition = Math.Round(excessOver450k * 0.15m, 2);
                }
            }
        }

        var irTotal = irPendingSalary + irOnVacation + irOnTrustPosition;
        var totalDeductions = inssLaboral + irTotal;

        // ── 7. Costos patronales (informativo, no se descuenta del empleado) ──
        // Se calculan sobre vacaciones (Art. 93 Reglamento Decreto 06-2019)
        var employerRate = taxConfig != null ? 
            (taxConfig.InssIntegralEmployerRateSmall > 0 ? taxConfig.InssIntegralEmployerRateSmall : 
             taxConfig.InssEmployerRate > 0 ? taxConfig.InssEmployerRate : 0.215m) : 0.215m;
        // Employer costs base = pendingSalary + vacation + overtime (all remunerative income)
        var employerCostBase = pendingSalaryPay + vacationPay + overtimePay;
        var inssPatronal = Math.Round(employerCostBase * employerRate, 2);
        var inatecRate = taxConfig != null ? taxConfig.OtherEmployerRate : 0.02m;
        var inatec = Math.Round(employerCostBase * inatecRate, 2);

        // ── Build settlement record ───────────────────────────────
        var grossSettlement = aguinaldoPay + vacationPay + severancePay
            + pendingSalaryPay + trustPositionPay;

        var record = new TerminationRecord
        {
            EmployeeId = employeeId,
            TerminationDate = terminationDate,
            Reason = reason,
            HireDate = employee.HireDate,
            DaysWorked = daysWorked,
            MonthsWorked = monthsWorked,
            YearsWorked = yearsWorked,
            DaysAccrued25PerMonth = daysAccrued25,
            MonthlySalary = monthlySalary,
            DailySalary = dailySalary,
            AguinaldoPay = aguinaldoPay,
            VacationDaysAccrued = daysAccrued25,
            VacationDaysTaken = vacationDaysTaken,
            VacationDaysToPay = vacationDaysToPay,
            VacationPay = vacationPay,
            SeveranceDays = severanceDays,
            SeverancePay = severancePay,
            IsTrustPosition = isTrustPosition,
            TrustPositionPay = trustPositionPay,
            PendingSalaryDays = pendingSalaryDays,
            PendingSalaryPay = pendingSalaryPay,
            OvertimeHours = overtimeHours,
            OvertimePay = overtimePay,
            InssLaboralAmount = inssLaboral,
            IrSalaryAmount = irPendingSalary,
            IrTotalAmount = irTotal,
            InssPatronalAmount = inssPatronal,
            InatecAmount = inatec,
            GrossSettlement = grossSettlement,
            TotalDeductions = totalDeductions,
            // ── Multi-country info for frontend ──
            CountryCode = employee.CountryCode,
            Currency = taxConfig?.Currency ?? "NIO",
            CountryName = taxConfig?.CountryName ?? employee.CountryCode,
            InssEmployeeRateDisplay = taxConfig?.InssEmployeeRate ?? 0.07m,
            InssEmployerRateDisplay = taxConfig?.InssIntegralEmployerRateSmall > 0
                ? taxConfig.InssIntegralEmployerRateSmall
                : taxConfig?.InssEmployerRate ?? 0.215m,
            OtherEmployerRateDisplay = taxConfig?.OtherEmployerRate ?? 0.02m,
            OtherEmployerName = taxConfig?.OtherEmployerName ?? "Seguro Social",
            Status = "draft"
        };

        await _repo.AddAsync(record);
        await _repo.SaveChangesAsync();
        return record;
    }

    /// <summary>
    /// Calculates monthly IR on ordinary salary using annual projection method (Art. 23 Ley 822).
    /// Formula: ((monthlySalary - INSS) × 12) → apply bracket table → ÷ 12
    /// </summary>
    public static decimal CalculateIrOnMonthlySalary(decimal monthlySalary, CountryTaxConfig? taxConfig)
    {
        if (taxConfig == null || monthlySalary <= 0) return 0m;

        var inssRate = taxConfig != null ? taxConfig.InssEmployeeRate : 0.07m;
        var taxableMonthly = monthlySalary - Math.Round(monthlySalary * inssRate, 2);
        var taxableAnnual = taxableMonthly * 12;

        var annualIr = CalculateAnnualIr(taxableAnnual, taxConfig);
        return Math.Round(annualIr / 12m, 2);
    }

    /// <summary>
    /// Calculates the incremental IR that an occasional payment generates (Art. 13.2 Reglamento Ley 822).
    /// Formula: IR(salary + payment) - IR(salary alone)
    /// </summary>
    public static decimal CalculateIncrementalIr(decimal monthlySalary, decimal occasionalPaymentNet, CountryTaxConfig? taxConfig)
    {
        if (taxConfig == null || monthlySalary <= 0 || occasionalPaymentNet <= 0) return 0m;

        var inssRate = taxConfig != null ? taxConfig.InssEmployeeRate : 0.07m;
        var taxableMonthly = monthlySalary - Math.Round(monthlySalary * inssRate, 2);
        var taxableAnnual = taxableMonthly * 12;

        // IR without the occasional payment
        var irWithout = CalculateAnnualIr(taxableAnnual, taxConfig);

        // IR with the occasional payment added to annual projection
        var irWith = CalculateAnnualIr(taxableAnnual + occasionalPaymentNet, taxConfig);

        return Math.Round(irWith - irWithout, 2);
    }

    /// <summary>
    /// Applies the progressive bracket table (Art. 23 Ley 822) to calculate annual IR.
    /// IR = FixedAmount + (TaxableIncome - BracketMin) × Rate
    /// where FixedAmount is the cumulative tax from previous brackets.
    /// </summary>
    public static decimal CalculateAnnualIr(decimal taxableAnnual, CountryTaxConfig taxConfig)
    {
        var brackets = ParseIrBrackets(taxConfig.IrTableJson);
        if (brackets.Count == 0) return 0m;

        // "Sobre Exceso de" (excessOver) = previous bracket's Max
        // First bracket uses 0 as the excess over (matching Excel Art. 23 Ley 822)
        decimal cumulativeTax = 0;
        decimal excessOver = 0;

        for (int i = 0; i < brackets.Count; i++)
        {
            var bracket = brackets[i];

            if (taxableAnnual >= bracket.Min && taxableAnnual <= bracket.Max)
            {
                return cumulativeTax + (taxableAnnual - excessOver) * bracket.Rate;
            }

            // Accumulate tax from this full bracket
            cumulativeTax += (bracket.Max - excessOver) * bracket.Rate;
            excessOver = bracket.Max;
        }

        // If above all brackets, apply the last bracket's rate on excess
        if (brackets.Count > 0 && taxableAnnual > brackets[^1].Max)
        {
            return cumulativeTax + (taxableAnnual - excessOver) * brackets[^1].Rate;
        }

        return 0m;
    }

    /// <summary>
    /// Gets vacation days already taken by the employee since hire date.
    /// Queries VacationRequest table for approved vacations in the employment period.
    /// </summary>
    private async Task<decimal> GetVacationDaysTakenAsync(Guid employeeId, DateOnly terminationDate)
    {
        try
        {
            // Use IVacationRepository.GetVacationDaysSumAsync for approved vacations
            return await _vacationRepo.GetVacationDaysSumAsync(employeeId, "approved");
        }
        catch
        {
            // If vacation repo not available, return 0 (will need manual adjustment)
            return 0;
        }
    }

    /// <summary>
    /// Calculates severance days using tiered formula (if configured) or flat formula.
    ///
    /// NIC Art. 45 CT from Excel formula =SI(G36>6;5*30;SI(G36>3;(3*30)+((G36-3)*20);G36*30)):
    ///   IF(years > 6): cap = 150 days
    ///   IF(years > 3): 90 + (years - 3) * 20
    ///   ELSE: years * 30
    /// </summary>
    internal static decimal CalculateSeveranceDays(
        int yearsOfService, CountryTaxConfig? taxConfig)
    {
        var tiers = ParseTiers(taxConfig?.IndemnityTiersJson);
        var maxDays = taxConfig?.MaxIndemnityDays ?? 0;

        decimal severanceDays;

        if (tiers.Count > 0 && maxDays > 0)
        {
            // Tiered calculation
            severanceDays = 0;
            var prevUpTo = 0;

            foreach (var tier in tiers)
            {
                if (yearsOfService <= prevUpTo) break;

                var tierSpan = tier.UpToYears - prevUpTo;
                var yearsInTier = Math.Min(yearsOfService - prevUpTo, tierSpan);
                severanceDays += yearsInTier * tier.DaysPerYear;
                prevUpTo = tier.UpToYears;
            }

            severanceDays = Math.Min(severanceDays, maxDays);
        }
        else
        {
            // Flat formula fallback
            var indemnityDaysPerYear = taxConfig?.IndemnityDaysPerYear ?? 30;
            var maxIndemnityYears = taxConfig?.MaxIndemnityYears ?? 12;
            severanceDays = Math.Min(yearsOfService * indemnityDaysPerYear, maxIndemnityYears * indemnityDaysPerYear);
        }

        return severanceDays;
    }

    private static List<IndemnityTier> ParseTiers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        try
        {
            return JsonSerializer.Deserialize<List<IndemnityTier>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<IrBracketEntry> ParseIrBrackets(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
        try
        {
            return JsonSerializer.Deserialize<List<IrBracketEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private sealed class IndemnityTier
    {
        public int UpToYears { get; set; }
        public int DaysPerYear { get; set; }
    }

    private sealed class IrBracketEntry
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Rate { get; set; }
    }
}
