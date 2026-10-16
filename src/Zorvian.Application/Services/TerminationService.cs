using System.Text.Json;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// Ciclo de vida del contrato de un trabajador: cálculo/aplicación de
/// liquidaciones y recontrataciones. Cada liquidación cierra el período de
/// contrato abierto (<see cref="EmploymentPeriod"/>); cada recontratación abre
/// uno nuevo sobre el MISMO empleado — el historial laboral se conserva entero
/// y la antigüedad del nuevo período arranca en su fecha de ingreso (la
/// indemnización del período anterior ya se pagó en su liquidación).
/// </summary>
public sealed class TerminationService
{
    private readonly ITerminationRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IBenefitProvisionRepository _benefitRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly IVacationRepository _vacationRepo;
    private readonly IPayrollRepository _payrollRepo;
    private readonly IEmploymentPeriodRepository _periodRepo;

    public TerminationService(
        ITerminationRepository repo,
        IEmployeeRepository employeeRepo,
        IBenefitProvisionRepository benefitRepo,
        ICountryTaxConfigRepository taxConfigRepo,
        IVacationRepository vacationRepo,
        IPayrollRepository payrollRepo,
        IEmploymentPeriodRepository periodRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _benefitRepo = benefitRepo;
        _taxConfigRepo = taxConfigRepo;
        _vacationRepo = vacationRepo;
        _payrollRepo = payrollRepo;
        _periodRepo = periodRepo;
    }

    /// <summary>
    /// Calculates the final termination settlement for an employee.
    /// </summary>
    /// <param name="employeeId">Employee identifier.</param>
    /// <param name="reason">Termination reason.</param>
    /// <param name="terminationDate">Date of termination.</param>
    /// <param name="paidThroughDate">If provided, employee is paid through this date (pending salary = 0).</param>
    /// <param name="overtimeHours">Overtime hours worked.</param>
    /// <param name="overtimePay">Overtime pay amount.</param>
    public async Task<TerminationRecord?> CalculateAsync(Guid employeeId, TerminationReason reason, DateOnly terminationDate, DateOnly? paidThroughDate = null, decimal overtimeHours = 0, decimal overtimePay = 0)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Employee not found");

        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(employee.CountryCode);
        var monthlySalary = employee.Salary ?? 0;
        var dailySalary = monthlySalary / 30m;

        // ── Salario variable: promedio de los últimos 6 meses (Art. 36 CT) ──
        // Si el tipo de salario es "variable", el salario base puede no reflejar
        // la remuneración real. Se calcula el promedio del GrossPay de los últimos
        // 6 meses de nómina procesada.
        var isVariableSalary = string.Equals(employee.SalaryType, "variable", StringComparison.OrdinalIgnoreCase);
        decimal? avgSalary6Months = null;
        decimal? maxSalary6Months = null;
        if (isVariableSalary)
        {
            var sixMonthsAgo = terminationDate.AddMonths(-6);
            var payrollDetails = await _payrollRepo.GetDetailsByEmployeeInRangeAsync(employeeId, sixMonthsAgo, terminationDate);
            if (payrollDetails.Count > 0)
            {
                avgSalary6Months = Math.Round(payrollDetails.Average(d => d.GrossPay), 2);
                maxSalary6Months = payrollDetails.Max(d => d.GrossPay);
                // Usar el promedio como salario para el cálculo de prestaciones
                monthlySalary = avgSalary6Months.Value;
                dailySalary = monthlySalary / 30m;
            }
        }

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

        // Renuncia Inmediata: pierde la indemnización por antigüedad (Art. 45 CT)
        if (daysWorked >= 30 && reason != TerminationReason.ImmediateResignation)
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
            IsVariableSalary = isVariableSalary,
            AvgSalary6Months = avgSalary6Months,
            MaxSalary6Months = maxSalary6Months,
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
    /// Aplica una liquidación calculada (draft → processed): marca al empleado
    /// como terminado, cierra su período de contrato abierto, desactiva el
    /// salario activo y liquida los saldos de vacaciones del año (los días
    /// devengados no tomados ya se pagaron en la liquidación — se iguala
    /// accrued = taken para que la recontratación arranque en cero y el
    /// VacationAutomatedJob no herede días ya cobrados).
    /// </summary>
    public async Task<TerminationRecord> ApplyTerminationAsync(Guid recordId)
    {
        var record = await _repo.GetByIdAsync(recordId)
            ?? throw new KeyNotFoundException("Liquidación no encontrada");
        if (record.Status == "processed")
            throw new InvalidOperationException("La liquidación ya fue aplicada.");

        var employee = await _employeeRepo.GetByIdAsync(record.EmployeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");

        // ── Empleado: terminado ──
        employee.Status = "terminated";
        employee.TerminationDate = record.TerminationDate;
        employee.TerminationReason = record.Reason.ToString();
        employee.History.Add(new EmployeeHistory
        {
            EmployeeId = employee.Id,
            ChangeType = "Terminate",
            FieldName = "Status",
            OldValue = "active",
            NewValue = "terminated",
        });

        // ── Período de contrato: cerrar el abierto (si existe) ──
        var openPeriod = await _periodRepo.GetOpenByEmployeeAsync(employee.Id);
        if (openPeriod is not null)
        {
            openPeriod.Status = "closed";
            openPeriod.TerminationDate = record.TerminationDate;
            openPeriod.TerminationReason = record.Reason.ToString();
            openPeriod.TerminationRecordId = record.Id;
            await _periodRepo.UpdateAsync(openPeriod);
        }

        // ── Salario activo: desactivar (la liquidación cierra la relación) ──
        var activeSalary = await _payrollRepo.GetActiveSalaryAsync(employee.Id);
        if (activeSalary is not null)
        {
            activeSalary.IsActive = false;
            await _payrollRepo.UpdateSalaryAsync(activeSalary);
        }

        // ── Saldos del año de salida: los devengados no tomados ya se pagaron ──
        var balance = await _vacationRepo.GetLeaveBalanceAsync(employee.Id, record.TerminationDate.Year);
        if (balance is not null)
        {
            balance.VacationDaysAccrued = balance.VacationDaysTaken;
            balance.VacationDaysPending = 0m;
            await _vacationRepo.SaveChangesAsync();
        }

        record.Status = "processed";
        await _repo.UpdateAsync(record);
        await _repo.SaveChangesAsync();
        await _employeeRepo.SaveChangesAsync();
        return record;
    }

    /// <summary>
    /// Recontrata a un trabajador dado de baja, sobre el MISMO registro de
    /// empleado (conserva códigos, documentos y historial). Abre un nuevo
    /// período de contrato con la nueva fecha de ingreso — la antigüedad
    /// reinicia porque el período anterior ya fue liquidado y pagado —,
    /// reactiva el estado, crea el salario activo del nuevo contrato y asegura
    /// los balances de vacaciones del año (en cero; el trabajo mensual los
    /// devenga).
    /// </summary>
    public async Task<Employee> RehireAsync(Guid employeeId, RehireEmployeeRequest request)
    {
        var employee = await _employeeRepo.GetByIdAsync(employeeId)
            ?? throw new KeyNotFoundException("Empleado no encontrado");
        if (employee.Status == "active")
            throw new InvalidOperationException("El trabajador ya está activo; no requiere recontratación.");

        var lastPeriod = (await _periodRepo.GetByEmployeeAsync(employeeId)).FirstOrDefault();
        if (lastPeriod is not null && lastPeriod.TerminationDate.HasValue && request.HireDate <= lastPeriod.TerminationDate.Value)
            throw new InvalidOperationException(
                $"La fecha de ingreso debe ser posterior a la salida del período anterior ({lastPeriod.TerminationDate:yyyy-MM-dd}).");

        // ── Nuevo período de contrato (antigüedad reinicia) ──
        var period = new EmploymentPeriod
        {
            EmployeeId = employee.Id,
            PeriodNumber = await _periodRepo.GetNextPeriodNumberAsync(employee.Id),
            HireDate = request.HireDate,
            BaseSalary = request.Salary ?? employee.Salary,
            Position = request.Position ?? employee.Position,
            Status = "open",
            Notes = request.Notes,
        };
        await _periodRepo.AddAsync(period);

        // ── Reactivar el empleado con los datos del nuevo contrato ──
        employee.HireDate = request.HireDate;
        employee.Status = "active";
        employee.TerminationDate = null;
        employee.TerminationReason = null;
        if (request.Salary.HasValue) employee.Salary = request.Salary;
        if (request.Position is not null) employee.Position = request.Position;
        if (request.DepartmentId.HasValue) employee.DepartmentId = request.DepartmentId;
        if (request.BranchId.HasValue) employee.BranchId = request.BranchId;
        employee.History.Add(new EmployeeHistory
        {
            EmployeeId = employee.Id,
            ChangeType = "Rehire",
            FieldName = "Status",
            OldValue = "terminated",
            NewValue = "active",
        });

        // ── Salario activo del nuevo contrato ──
        var newSalary = request.Salary ?? employee.Salary ?? 0m;
        if (newSalary > 0)
        {
            await _payrollRepo.AddSalaryAsync(new EmployeeSalary
            {
                EmployeeId = employee.Id,
                BaseSalary = newSalary,
                SalaryType = employee.SalaryType ?? "monthly",
                EffectiveDate = request.HireDate,
                IsActive = true,
                Notes = $"Recontratación — período {period.PeriodNumber}",
            });
        }

        // ── Balances del año de reingreso (en cero; el job mensual devenga) ──
        var balance = await _vacationRepo.GetLeaveBalanceAsync(employee.Id, request.HireDate.Year);
        if (balance is null)
        {
            await _vacationRepo.AddLeaveBalanceAsync(new LeaveBalances
            {
                EmployeeId = employee.Id,
                Year = request.HireDate.Year,
                VacationDaysAccrued = 0m,
                VacationDaysTaken = 0m,
                VacationDaysPending = 0m,
                SickDaysAccrued = 0m,
                SickDaysTaken = 0m,
                PersonalDaysAccrued = 0m,
                PersonalDaysTaken = 0m,
            });
        }

        await _periodRepo.SaveChangesAsync();
        await _payrollRepo.SaveChangesAsync();
        await _vacationRepo.SaveChangesAsync();
        await _employeeRepo.SaveChangesAsync();
        return employee;
    }

    /// <summary>Historial de contratos de un trabajador (períodos, más reciente primero).</summary>
    public async Task<List<EmploymentPeriod>> GetEmploymentPeriodsAsync(Guid employeeId) =>
        await _periodRepo.GetByEmployeeAsync(employeeId);

    /// <summary>
    /// Calculates monthly IR on ordinary salary using annual projection method (Art. 23 Ley 822).
    /// Formula: ((monthlySalary - INSS) × 12) → apply bracket table → ÷ 12
    /// </summary>
    public static decimal CalculateIrOnMonthlySalary(decimal monthlySalary, CountryTaxConfig? taxConfig)
    {
        if (taxConfig == null || monthlySalary <= 0) return 0m;

        var inssRate = taxConfig.InssEmployeeRate;
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

        var inssRate = taxConfig.InssEmployeeRate;
        var taxableMonthly = monthlySalary - Math.Round(monthlySalary * inssRate, 2);
        var taxableAnnual = taxableMonthly * 12;

        var irWithout = CalculateAnnualIr(taxableAnnual, taxConfig);

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
            // Sum only vacations where the employee ACTUALLY took the days off.
            // An approved vacation for FUTURE dates hasn't been taken yet —
            // those days must be PAID in the settlement, not deducted.
            return await _vacationRepo.GetVacationDaysTakenBeforeAsync(employeeId, terminationDate);
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
