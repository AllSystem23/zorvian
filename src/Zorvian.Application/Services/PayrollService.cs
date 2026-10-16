using System.Text.Json;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PayrollService
{
    private readonly IPayrollRepository _repo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;
    private readonly IWebhookService _webhookService;
    private readonly IAchExportService _achService;
    private readonly IAutoAccountingService _autoAccounting;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IOvertimeRecordRepository _overtimeRepo;
    private readonly ICommissionRecordRepository _commissionRepo;
    private readonly IBonusRecordRepository _bonusRepo;
    private readonly IPayrollConceptRepository _conceptRepo;
    private readonly IEmployeeLoanRepository _loanRepo;
    private readonly ISalaryAdvanceRepository _advanceRepo;
    private readonly IWageGarnishmentRepository _garnishmentRepo;
    private readonly IBenefitProvisionRepository _benefitProvisionRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly PayrollCalculationFactory _calcFactory;
    private readonly IBankTransferService _bankTransferService;
    private readonly ISickLeaveRepository _sickLeaveRepo;
    private readonly IVacationRepository _vacationRepo;
    private readonly IPermissionRepository _permissionRepo;

    public PayrollService(
        IPayrollRepository repo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        IWebhookService webhookService,
        IAchExportService achService,
        IAutoAccountingService autoAccounting,
        ICountryTaxConfigRepository taxConfigRepo,
        ICompanyRepository companyRepo,
        IOvertimeRecordRepository overtimeRepo,
        ICommissionRecordRepository commissionRepo,
        IBonusRecordRepository bonusRepo,
        IPayrollConceptRepository conceptRepo,
        IEmployeeLoanRepository loanRepo,
        ISalaryAdvanceRepository advanceRepo,
        IWageGarnishmentRepository garnishmentRepo,
        IBenefitProvisionRepository benefitProvisionRepo,
        IAuditLogRepository auditRepo,
        PayrollCalculationFactory calcFactory,
        IBankTransferService bankTransferService,
        ISickLeaveRepository sickLeaveRepo,
        IVacationRepository vacationRepo,
        IPermissionRepository permissionRepo)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _webhookService = webhookService;
        _achService = achService;
        _autoAccounting = autoAccounting;
        _taxConfigRepo = taxConfigRepo;
        _companyRepo = companyRepo;
        _overtimeRepo = overtimeRepo;
        _commissionRepo = commissionRepo;
        _bonusRepo = bonusRepo;
        _conceptRepo = conceptRepo;
        _loanRepo = loanRepo;
        _advanceRepo = advanceRepo;
        _garnishmentRepo = garnishmentRepo;
        _benefitProvisionRepo = benefitProvisionRepo;
        _auditRepo = auditRepo;
        _calcFactory = calcFactory;
        _bankTransferService = bankTransferService;
        _sickLeaveRepo = sickLeaveRepo;
        _vacationRepo = vacationRepo;
        _permissionRepo = permissionRepo;
    }

    private Guid RequireCompanyId()
    {
        if (_tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Seleccione una empresa antes de procesar nómina.");

        return _tenant.TenantId.Value;
    }

    // --- Deduction Types ---

    public async Task<List<DeductionTypeResponse>> GetDeductionTypesAsync()
    {
        var items = await _repo.GetDeductionTypesAsync();
        return items.Select(MapDeductionType).ToList();
    }

    public async Task<DeductionTypeResponse> CreateDeductionTypeAsync(CreateDeductionTypeRequest request)
    {
        var entity = new DeductionType
        {
            Code = request.Code,
            Name = request.Name,
            CalculationMethod = request.CalculationMethod,
            Rate = request.Rate,
            FixedAmount = request.FixedAmount,
            IsMandatory = request.IsMandatory,
            IsActive = true,
            Priority = request.Priority,
        };
        await _repo.AddDeductionTypeAsync(entity);
        await _repo.SaveChangesAsync();
        return MapDeductionType(entity);
    }

    public async Task<DeductionTypeResponse?> UpdateDeductionTypeAsync(Guid id, UpdateDeductionTypeRequest request)
    {
        var entity = await _repo.GetDeductionTypeByIdAsync(id);
        if (entity is null) return null;
        if (request.Name != null) entity.Name = request.Name;
        if (request.Rate.HasValue) entity.Rate = request.Rate;
        if (request.FixedAmount.HasValue) entity.FixedAmount = request.FixedAmount;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
        if (request.Priority.HasValue) entity.Priority = request.Priority;
        await _repo.UpdateDeductionTypeAsync(entity);
        await _repo.SaveChangesAsync();
        return MapDeductionType(entity);
    }

    public async Task<bool> DeleteDeductionTypeAsync(Guid id)
    {
        var entity = await _repo.GetDeductionTypeByIdAsync(id);
        if (entity is null) return false;
        await _repo.DeleteDeductionTypeAsync(id);
        await _repo.SaveChangesAsync();
        return true;
    }

    // --- Employee Salaries ---

    public async Task<List<EmployeeSalaryResponse>> GetSalariesAsync(Guid? employeeId)
    {
        var items = await _repo.GetSalariesAsync(employeeId);
        return items.Select(MapSalary).ToList();
    }

    public async Task<EmployeeSalaryResponse> CreateSalaryAsync(CreateEmployeeSalaryRequest request)
    {
        var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId)
            ?? throw new InvalidOperationException("Employee not found");

        var active = await _repo.GetActiveSalaryAsync(request.EmployeeId);
        if (active != null)
        {
            active.IsActive = false;
            active.EndDate = request.EffectiveDate.AddDays(-1);
            await _repo.UpdateSalaryAsync(active);
        }

        var entity = new EmployeeSalary
        {
            EmployeeId = request.EmployeeId,
            BaseSalary = request.BaseSalary,
            SalaryType = request.SalaryType,
            EffectiveDate = request.EffectiveDate,
            IsActive = true,
            Notes = request.Notes,
        };
        await _repo.AddSalaryAsync(entity);
        await _repo.SaveChangesAsync();
        return MapSalary(entity);
    }

    public async Task<bool> DeactivateSalaryAsync(Guid id)
    {
        var salary = await _repo.GetSalaryByIdAsync(id);
        if (salary is null) return false;
        salary.IsActive = false;
        salary.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await _repo.UpdateSalaryAsync(salary);
        await _repo.SaveChangesAsync();
        return true;
    }

    // --- Payroll Periods ---

    public async Task<List<PayrollPeriodResponse>> GetPeriodsAsync(int? year)
    {
        var items = await _repo.GetPeriodsAsync(year);
        return items.Select(MapPeriod).ToList();
    }

    public async Task<PayrollPeriodResponse> CreatePeriodAsync(CreatePayrollPeriodRequest request)
    {
        var entity = new PayrollPeriod
        {
            Name = request.Name,
            Year = request.Year,
            Month = request.Month,
            PeriodNumber = request.PeriodNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PaymentDate = request.PaymentDate,
            Status = "open",
            TenantId = _tenant.TenantId.ToString(),
        };
        await _repo.AddPeriodAsync(entity);
        await _repo.SaveChangesAsync();
        return MapPeriod(entity);
    }

    // --- Payroll Processing ---

    public async Task<List<PayrollRunResponse>> GetRunsAsync(Guid? periodId)
    {
        var items = await _repo.GetRunsAsync(periodId);
        return items.Select(MapRun).ToList();
    }

    public async Task<PayrollRunResponse?> GetRunByIdAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        return run is null ? null : MapRun(run);
    }

    public async Task<PayrollRunResponse> GeneratePayrollAsync(GeneratePayrollRequest request)
    {
        var period = await _repo.GetPeriodByIdAsync(request.PayrollPeriodId)
            ?? throw new KeyNotFoundException("Payroll period not found");
        if (period.Status != "open")
            throw new InvalidOperationException("Solo se pueden generar corridas para períodos abiertos.");

        // Una segunda corrida para el mismo período pagaría salarios dos veces e incluiría
        // vacationPay de vacaciones que otra corrida descontará del saldo al aprobarse.
        // Hay que borrar (borrador) o cancelar la corrida existente antes: la cancelación
        // reabre el período y revierte las vacaciones que hubiera consumido.
        var existingRuns = await _repo.GetRunsAsync(request.PayrollPeriodId) ?? new List<PayrollRun>();
        if (existingRuns.Any(r => r.Status != "cancelled"))
            throw new InvalidOperationException(
                "El período ya tiene una corrida de nómina. Elimine o cancele la corrida existente antes de regenerar.");

        var companyId = RequireCompanyId();
        var company = await _companyRepo.GetByTenantIdAsync(companyId.ToString()) 
            ?? throw new KeyNotFoundException("Company not found for selected tenant.");
        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(company.Country ?? "NIC")
            ?? throw new KeyNotFoundException("Tax configuration not found for country");

        var activeEmployees = await _employeeRepo.GetFilteredAsync(null, "active", null, 1, 10000);
        if (activeEmployees.Count == 0)
            throw new InvalidOperationException("No hay empleados activos para procesar nómina.");

        // Resolver tasas INSS según régimen de la empresa y cantidad de empleados
        var companySettings = await _companyRepo.GetSettingsAsync(company.Id);
        var inssRates = ResolveInssRates(taxConfig, companySettings, activeEmployees.Count);

        var conceptDefinitions = await _conceptRepo.GetAllAsync(_tenant.TenantId);
        var strategy = _calcFactory.GetStrategy(company.Country ?? "NIC");

        // Fetch variable concepts once per payroll run
        var overtimeRecords = await _overtimeRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<OvertimeRecord>();
        var commissionRecords = await _commissionRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<CommissionRecord>();
        var bonusRecords = await _bonusRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<BonusRecord>();

        // Permisos aprobados que se solapan con el período. Días CON goce (LeaveType.IsPaid = true)
        // restauran salario (asistencia médica/personal/pagada: no son falta); días SIN goce no
        // suman y el salario queda prorrateado por ellos (equivalente al descuento "sin goce").
        var permissionRecords = await _permissionRepo.GetApprovedOverlappingAsync(period.StartDate, period.EndDate)
            ?? new List<PermissionRequest>();

        var totalSalaries = 0m;
        var totalDeductions = 0m;
        var totalEmployerCosts = 0m;
        var details = new List<PayrollDetail>();

        foreach (var emp in activeEmployees)
        {
            var salary = await _repo.GetActiveSalaryAsync(emp.Id);
            var baseSalary = salary?.BaseSalary ?? emp.Salary ?? 0;

            var empOvertime = overtimeRecords.Where(o => o.EmployeeId == emp.Id).Sum(o => o.Amount);
            var empCommission = commissionRecords.Where(c => c.EmployeeId == emp.Id).Sum(c => c.Amount);
            var empBonus = bonusRecords.Where(b => b.EmployeeId == emp.Id).Sum(b => b.Amount);

            // Deducciones automáticas
            var loans = await _loanRepo.GetActiveLoansAsync(emp.Id, _tenant.TenantId);
            var advances = await _advanceRepo.GetByEmployeeAsync(emp.Id, _tenant.TenantId);
            var garnishments = await _garnishmentRepo.GetActiveGarnishmentsAsync(emp.Id, _tenant.TenantId);

            var loanDeduction = loans.Sum(l => l.InstallmentAmount);
            var advanceDeduction = advances.Where(a => a.Status == "approved").Sum(a => a.DeductionPerPeriod ?? 0);

            // Attendance Integration (H-AL-06)
            var attendanceRecords = await _employeeRepo.GetAttendanceInRangeAsync(emp.Id, period.StartDate, period.EndDate);
            var expectedDays = (period.EndDate.ToDateTime(TimeOnly.MinValue) - period.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            var workedDays = attendanceRecords.Count(a => a.Status == "present" || a.Status == "late");

            // Permisos aprobados de ESTE empleado dentro del período (medio día = 0.5).
            decimal paidPermissionDays = 0m;
            foreach (var perm in permissionRecords.Where(p => p.EmployeeId == emp.Id))
            {
                var ovStart = perm.StartDate > period.StartDate ? perm.StartDate : period.StartDate;
                var ovEnd = perm.EndDate < period.EndDate ? perm.EndDate : period.EndDate;
                if (ovStart > ovEnd) continue;
                var days = perm.IsHalfDay ? 0.5m : CountBusinessDays(ovStart, ovEnd);
                if (perm.LeaveType.IsPaid) paidPermissionDays += days;
            }

            // Adjust base salary if not full attendance (simple logic: proportional to days).
            // Base de cálculo: días con derecho a salario = asistencia + permisos con goce
            // (tope en días del período). Los permisos sin goce y las ausencias no excusadas
            // no suman → prorratean como antes.
            var adjustedBaseSalary = baseSalary;
            if (emp.SalaryType == "hourly")
            {
                var totalHours = attendanceRecords.Sum(a => a.TotalHours ?? 0m);
                adjustedBaseSalary = totalHours * (baseSalary); // baseSalary here acts as hourly rate
            }
            else
            {
                var daysWithRightToSalary = Math.Min((decimal)expectedDays, workedDays + paidPermissionDays);
                if (daysWithRightToSalary < expectedDays && expectedDays > 0)
                {
                    adjustedBaseSalary = (baseSalary / expectedDays) * daysWithRightToSalary;
                }
            }

            // Vacation Payment (H-BA-03)
            var approvedVacations = await _employeeRepo.GetVacationsInRangeAsync(emp.Id, period.StartDate, period.EndDate);
            var vacationPay = approvedVacations.Where(v => v.Status == "approved").Sum(v => v.BusinessDays * (baseSalary / 30));

            var grossPay = adjustedBaseSalary + empOvertime + empCommission + empBonus + vacationPay;

            // Base de retenciones de ley (INSS/IR): remuneración normal gravable por defecto
            // (salario devengado + variables; EXCLUYE vacaciones — cumplimiento Ley 861/CCSS/IGSS/ISSS/IHSS/CSS).
            // Configurable por empresa vía CompanySettings.RetentionBase ("base_salary" = solo salario base).
            var retentionBase = companySettings?.RetentionBase == "base_salary"
                ? adjustedBaseSalary
                : grossPay - vacationPay;

            // INSS empleado: usar tasa resuelta por régimen.
            // Trabajador no cotizante (Employee.DeductInss = false, ej. pensionado del INSS en NIC):
            // sin cuota del trabajador NI patronal; el IR se calcula igual con INSS = 0
            // (base gravable completa). Paridad con TerminationService, que ya lo respeta en
            // liquidaciones — el switch del formulario de empleado surte efecto real aquí.
            var deductInss = emp.DeductInss;
            var inssDeduction = deductInss
                ? Math.Min(retentionBase * inssRates.EmployeeRate, taxConfig.InssEmployeeMax)
                : 0m;
            var irDeduction = strategy.CalculateIr(retentionBase, inssDeduction, taxConfig);
            
            var garnishmentDeduction = garnishments.Sum(g => g.GarnishmentType == "fixed" ? g.Value : grossPay * (g.Value / 100));

            // Cálculo de costos patronales: usar tasa resuelta por régimen + tamaño.
            // El aporte patronal usa la MISMA base gravable legal que la retención al trabajador.
            var employerInss = deductInss
                ? Math.Min(retentionBase * inssRates.EmployerRate, taxConfig.InssEmployerMax)
                : 0m;
            var otherEmployerCosts = grossPay * taxConfig.OtherEmployerRate;
            var totalEmployerCost = employerInss + otherEmployerCosts;

            var totalEmpDeductions = inssDeduction + irDeduction + loanDeduction + advanceDeduction + garnishmentDeduction;

            var concepts = new List<PayrollDetailConcept>();
            var tenantId = companyId;

            void AddConcept(string code, decimal amount, bool isEmployerCost = false)
            {
                var def = conceptDefinitions.FirstOrDefault(c => c.Code == code);
                concepts.Add(new PayrollDetailConcept
                {
                    ConceptCode = code,
                    Description = def?.Name ?? code,
                    Amount = amount,
                    IsEmployerCost = isEmployerCost,
                    CompanyId = tenantId
                });
            }

            AddConcept("SALARY", adjustedBaseSalary);
            if (vacationPay > 0) AddConcept("VACATION_PAY", vacationPay);
            // No crea filas de INSS en 0 (trabajador no cotizante): la colilla no debe
            // mostrar "INSS: 0.00" como si hubiera una retención aplicada.
            if (inssDeduction > 0) AddConcept("INSS_EMP", inssDeduction);
            AddConcept("IR", irDeduction);
            if (employerInss > 0) AddConcept("INSS_PAT", employerInss, true);

            if (otherEmployerCosts > 0) AddConcept("OTHER_PAT", otherEmployerCosts, true);
            if (empOvertime > 0) AddConcept("OVERTIME", empOvertime);
            if (empCommission > 0) AddConcept("COMMISSION", empCommission);
            if (empBonus > 0) AddConcept("BONUS", empBonus);
            if (loanDeduction > 0) AddConcept("LOAN", loanDeduction);
            if (advanceDeduction > 0) AddConcept("ADVANCE", advanceDeduction);
            if (garnishmentDeduction > 0) AddConcept("GARNISHMENT", garnishmentDeduction);

            var detail = new PayrollDetail
            {
                EmployeeId = emp.Id,
                BaseSalary = baseSalary,
                GrossPay = grossPay,
                TotalDeductions = totalEmpDeductions,
                NetPay = grossPay - totalEmpDeductions,
                InssCode = emp.IdentificationNumber,
                InssDeduction = inssDeduction,
                IrDeduction = irDeduction,
                OtherDeductions = loanDeduction + advanceDeduction + garnishmentDeduction,
                // Escalares de ingresos variables: la Colilla de Pago los imprime como claves
                // (commissions/bonuses/overtime) — sin esto imprimían 0.00 aunque existieran conceptos.
                CommissionsAmount = empCommission,
                BonusesAmount = empBonus,
                OvertimeAmount = empOvertime,
                Details = "{}",
                Concepts = concepts,
                Currency = company.Currency ?? "NIO",
                ExchangeRate = 1.0m // Placeholder for multi-currency logic
            };

            // Provisiones (Cálculos simples basados en política de país)
            var vacationProvision = grossPay * (1m / 12m); // Proporcional básico
            var aguinaldoProvision = grossPay * taxConfig.ChristmasBonusPercentage;

            await _benefitProvisionRepo.AddAsync(new BenefitProvision {
                EmployeeId = emp.Id, BenefitType = "vacation", Amount = vacationProvision, 
                CalculationDate = DateOnly.FromDateTime(DateTime.UtcNow), PayrollPeriodId = period.Id, TenantId = _tenant.TenantId
            });
            await _benefitProvisionRepo.AddAsync(new BenefitProvision {
                EmployeeId = emp.Id, BenefitType = "aguinaldo", Amount = aguinaldoProvision, 
                CalculationDate = DateOnly.FromDateTime(DateTime.UtcNow), PayrollPeriodId = period.Id, TenantId = _tenant.TenantId
            });

            details.Add(detail);

            totalSalaries += grossPay;
            totalDeductions += totalEmpDeductions;
            totalEmployerCosts += totalEmployerCost + vacationProvision + aguinaldoProvision;
        }

        await _benefitProvisionRepo.SaveChangesAsync();
        var run = new PayrollRun
        {
            PayrollPeriodId = request.PayrollPeriodId,
            Status = "draft",
            TotalSalaries = totalSalaries,
            TotalDeductions = totalDeductions,
            TotalNetPay = totalSalaries - totalDeductions,
            TotalEmployerCosts = totalEmployerCosts,
            EmployeeCount = details.Count,
            Notes = request.Notes,
            TenantId = _tenant.TenantId.ToString(),
        };

        await _repo.AddRunAsync(run);
        await _repo.SaveChangesAsync();

        foreach (var d in details)
        {
            d.PayrollRunId = run.Id;
            d.TenantId = _tenant.TenantId.ToString();
        }
        run.Details = details;
        await _repo.AddDetailsAsync(details);
        await _repo.SaveChangesAsync();

        return MapRun(run);
    }

    public async Task<PayrollRunResponse?> ApproveRunAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        if (run is null) return null;

        if (run.Status == "draft")
        {
            // Transition to pending_approval and initialize steps based on amount
            run.Status = "pending_approval";
            
            // Simple multi-level logic based on TotalNetPay
            int requiredSteps = 1;
            if (run.TotalNetPay > 10000) requiredSteps = 2;
            if (run.TotalNetPay > 50000) requiredSteps = 3;

            for (int i = 1; i <= requiredSteps; i++)
            {
                run.ApprovalSteps.Add(new ApprovalFlow
                {
                    RequestType = "payroll",
                    RequestId = run.Id,
                    Step = i,
                    Status = "pending",
                });
            }
            await _repo.UpdateRunAsync(run);
            await _repo.SaveChangesAsync();

            await _webhookService.PublishAsync(run.TenantId, "payroll.submitted", new { RunId = run.Id, RequiredSteps = requiredSteps });
            return await GetRunByIdAsync(run.Id);
        }

        if (run.Status == "pending_approval")
        {
            var currentStep = run.ApprovalSteps
                .Where(s => s.Status == "pending")
                .OrderBy(s => s.Step)
                .FirstOrDefault();

            if (currentStep != null)
            {
                currentStep.Status = "approved";
                currentStep.ApprovedAt = DateTime.UtcNow;
                currentStep.ApproverId = _tenant.CurrentEmployeeId;
                
                // Check if all steps are approved
                if (run.ApprovalSteps.All(s => s.Status == "approved"))
                {
                    run.Status = "approved";
                    run.ProcessedAt = DateTime.UtcNow;
                    run.ProcessedBy = _tenant.CurrentEmployeeId?.ToString() ?? "system";

                    var period = await _repo.GetPeriodByIdAsync(run.PayrollPeriodId);
                    if (period != null)
                    {
                        period.Status = "closed";
                        await _repo.UpdatePeriodAsync(period);

                        // Vacaciones pagadas por esta corrida: las aprobadas del período pasan a
                        // "taken" y descuentan el saldo (recálculo idempotente vía LeaveBalanceSync).
                        // PaidByPayrollRunId las identifica para revertir el consumo si la corrida
                        // se cancela, e impide que después se paguen también por recibo RV.
                        var runVacations = await _vacationRepo.GetApprovedInRangeAsync(period.StartDate, period.EndDate)
                            ?? new List<VacationRequest>();
                        foreach (var v in runVacations)
                        {
                            v.Status = "taken";
                            v.PaidByPayrollRunId = run.Id;
                        }
                        foreach (var g in runVacations.GroupBy(v => new { v.EmployeeId, Year = v.StartDate.Year }))
                            await LeaveBalanceSync.SyncAsync(_vacationRepo, _tenant, g.Key.EmployeeId, g.Key.Year);
                    }

                    // Generate accounting entry
                    await _autoAccounting.GeneratePayrollEntryAsync(run.Id, branchId: _tenant.ResolveBranchId());

                    // Notify via Webhook
                    await _webhookService.PublishAsync(run.TenantId, "payroll.approved", new { RunId = run.Id, TotalNetPay = run.TotalNetPay });
                }

                await _repo.UpdateRunAsync(run);
                await _repo.SaveChangesAsync();
                return await GetRunByIdAsync(run.Id);
            }
        }

        throw new InvalidOperationException($"Cannot approve run in status '{run.Status}'");
    }

    public async Task<PayrollRunResponse?> MarkAsPaidAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        if (run is null) return null;
        if (run.Status != "approved")
            throw new InvalidOperationException("Only approved runs can be marked as paid");

        // Simulation: Process each detail
        foreach (var detail in run.Details)
        {
            var bankAccounts = await _employeeRepo.GetBankAccountsAsync(detail.EmployeeId);
            if (bankAccounts.Any(ba => ba.IsActive))
            {
                detail.PaymentStatus = "paid";
                detail.PaymentReference = $"SIM-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            }
            else
            {
                detail.PaymentStatus = "failed";
                detail.Details = "No active bank account found for employee";
            }
            await _repo.UpdateDetailAsync(detail);
        }

        run.Status = "paid";
        await _repo.UpdateRunAsync(run);
        await _repo.SaveChangesAsync();

        await _webhookService.PublishAsync(run.TenantId, "payroll.paid", new { 
            RunId = run.Id, 
            TotalPaid = run.Details.Where(d => d.PaymentStatus == "paid").Sum(d => d.NetPay),
            FailedCount = run.Details.Count(d => d.PaymentStatus == "failed")
        });

        return await GetRunByIdAsync(run.Id);
    }

    private static decimal CalculateInss(decimal grossPay, CountryTaxConfig config)
    {
        var inss = grossPay * config.InssEmployeeRate;
        return Math.Min(inss, config.InssEmployeeMax);
    }

    private record IrBracket(decimal Min, decimal Max, decimal Rate, decimal FixedAmount);

    private static decimal CalculateIr(decimal taxableIncome, CountryTaxConfig config)
    {
        var brackets = JsonSerializer.Deserialize<List<IrBracket>>(config.IrTableJson) 
                       ?? new List<IrBracket>();
        
        var bracket = brackets.FirstOrDefault(b => taxableIncome >= b.Min && taxableIncome <= b.Max)
                      ?? brackets.LastOrDefault(b => taxableIncome > b.Max)
                      ?? brackets.FirstOrDefault();

        if (bracket == null) return 0m;
        
        return bracket.FixedAmount + (taxableIncome - bracket.Min) * bracket.Rate;
    }

    /// <summary>
    /// Resolves effective INSS employee and employer rates based on company regime and employee count.
    /// For countries like Nicaragua (Decreto 06-2019), rates differ by:
    ///   - Regime: Integral (general employment) vs IVM (disability, old age, death)
    ///   - Company size: 50+ employees (standard) vs less than 50 (reduced rate)
    /// Falls back to legacy single-rate fields if no regime data is configured.
    /// </summary>
    private static (decimal EmployeeRate, decimal EmployerRate) ResolveInssRates(
        CountryTaxConfig taxConfig, CompanySettings? settings, int activeEmployeeCount)
    {
        var regime = settings?.InssRegime ?? "integral";
        var isSmallEmployer = activeEmployeeCount < taxConfig.InssSmallEmployerThreshold;

        // Try regime-specific rates first
        return regime.ToLower() switch
        {
            "ivm" when taxConfig.InssIvmEmployeeRate > 0 => (
                EmployeeRate: taxConfig.InssIvmEmployeeRate,
                EmployerRate: isSmallEmployer ? taxConfig.InssIvmEmployerRateSmall : taxConfig.InssIvmEmployerRate
            ),
            "integral" when taxConfig.InssIntegralEmployeeRate > 0 => (
                EmployeeRate: taxConfig.InssIntegralEmployeeRate,
                EmployerRate: isSmallEmployer ? taxConfig.InssIntegralEmployerRateSmall : taxConfig.InssIntegralEmployerRate
            ),
            _ => (
                // Fallback: legacy single-rate fields
                EmployeeRate: taxConfig.InssEmployeeRate,
                EmployerRate: taxConfig.InssEmployerRate
            )
        };
    }

    /// <summary>
    /// Días hábiles (lun–vie) de un rango inclusive — misma regla que PermissionService
    /// usa al calcular BusinessDays de un permiso.
    /// </summary>
    private static int CountBusinessDays(DateOnly start, DateOnly end)
    {
        int count = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }

    public async Task<bool> DeleteRunAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        if (run is null) return false;
        if (run.Status != "draft")
            throw new InvalidOperationException("Only draft runs can be deleted");
        await _repo.DeleteRunAsync(id);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<PayrollRunResponse?> CancelRunAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        if (run is null) return null;
        if (run.Status == "cancelled")
            throw new InvalidOperationException("Run is already cancelled");
        run.Status = "cancelled";
        await _repo.UpdateRunAsync(run);
        await _repo.SaveChangesAsync();

        // Revierte las vacaciones que esta corrida consumió al aprobarse: vuelven a "approved"
        // (disponibles para pago por recibo o nueva corrida) y se recalculan sus espejos de saldo.
        // Vacaciones marcadas por otros caminos (recibo RV, "marcar como tomada") no tienen
        // PaidByPayrollRunId y por tanto no se tocan.
        var runVacations = await _vacationRepo.GetByPayrollRunAsync(run.Id) ?? new List<VacationRequest>();
        if (runVacations.Count > 0)
        {
            foreach (var v in runVacations)
            {
                v.Status = "approved";
                v.PaidByPayrollRunId = null;
            }
            foreach (var g in runVacations.GroupBy(v => new { v.EmployeeId, Year = v.StartDate.Year }))
                await LeaveBalanceSync.SyncAsync(_vacationRepo, _tenant, g.Key.EmployeeId, g.Key.Year);
        }

        // Re-open the period if it was closed by approval
        var period = await _repo.GetPeriodByIdAsync(run.PayrollPeriodId);
        if (period != null && period.Status == "closed")
        {
            period.Status = "open";
            await _repo.UpdatePeriodAsync(period);
            await _repo.SaveChangesAsync();
        }

        return MapRun(run);
    }

    public async Task<PayrollDetailResponse?> UpdateDetailAsync(Guid detailId, UpdatePayrollDetailRequest request)
    {
        var detail = await _repo.GetDetailByIdAsync(detailId);
        if (detail is null) return null;

        var oldValues = JsonSerializer.Serialize(new {
            detail.BaseSalary, detail.GrossPay, detail.TotalDeductions, detail.NetPay,
            detail.InssDeduction, detail.IrDeduction, detail.OtherDeductions
        });

        if (request.BaseSalary.HasValue) detail.BaseSalary = request.BaseSalary.Value;
        if (request.GrossPay.HasValue) detail.GrossPay = request.GrossPay.Value;
        if (request.TotalDeductions.HasValue) detail.TotalDeductions = request.TotalDeductions.Value;
        if (request.NetPay.HasValue) detail.NetPay = request.NetPay.Value;
        if (request.InssDeduction.HasValue) detail.InssDeduction = request.InssDeduction.Value;
        if (request.IrDeduction.HasValue) detail.IrDeduction = request.IrDeduction.Value;
        if (request.OtherDeductions.HasValue) detail.OtherDeductions = request.OtherDeductions.Value;
        if (request.Details != null) detail.Details = request.Details;

        // Recalculate totals
        detail.TotalDeductions = (detail.InssDeduction ?? 0) + (detail.IrDeduction ?? 0) + (detail.OtherDeductions ?? 0);
        detail.NetPay = detail.GrossPay - detail.TotalDeductions;

        // Sincroniza los conceptos con los valores editados: la Colilla de Pago imprime las
        // deducciones desde Concepts ({% for c in concepts %}), NO los escalares — sin este
        // sync la Planilla mostraba el valor editado y la Colilla el calculado original.
        await SyncConceptsAsync(detail);

        await _repo.UpdateDetailAsync(detail);
        await _repo.SaveChangesAsync();

        var newValues = JsonSerializer.Serialize(new {
            detail.BaseSalary, detail.GrossPay, detail.TotalDeductions, detail.NetPay,
            detail.InssDeduction, detail.IrDeduction, detail.OtherDeductions
        });

        await _auditRepo.AddAsync(new AuditLog
        {
            EntityName = "PayrollDetail",
            EntityId = detail.Id.ToString(),
            Action = "UPDATE",
            OldValues = oldValues,
            NewValues = newValues,
            PerformedBy = _tenant.CurrentEmployeeId,
            CreatedAt = DateTime.UtcNow
        });

        return new PayrollDetailResponse(
            detail.Id, detail.EmployeeId,
            detail.Employee?.FirstName + " " + detail.Employee?.LastName ?? "",
            detail.Employee?.EmployeeCode ?? "",
            detail.BaseSalary, detail.GrossPay, detail.TotalDeductions, detail.NetPay,
            detail.InssCode, detail.InssDeduction, detail.IrDeduction, detail.OtherDeductions,
            detail.PaymentStatus, detail.Currency,
            detail.Concepts?.Select(c => new PayrollDetailConceptResponse(c.ConceptCode, c.Description, c.Amount, c.IsEmployerCost)).ToList()
        );
    }

    /// <summary>
    /// Sincroniza las filas de PayrollDetailConcept con los escalares editados del detalle.
    /// La Colilla de Pago renderiza sus deducciones desde Concepts, y la Planilla desde los
    /// escalares: ambos documentos deben coincidir. Además refresca las claves escalares de
    /// ingresos variables (commissions/bonuses/overtime) que la Colilla imprime como filas de
    /// "Ingresos". La diferencia entre "Otras deducciones" editadas y sus conceptos
    /// (LOAN/ADVANCE/GARNISHMENT/…) se vuelca en un concepto ADJUSTMENT.
    /// </summary>
    private async Task SyncConceptsAsync(PayrollDetail detail)
    {
        // ?? new List: mismo patrón defensivo del resto del archivo — un repo/mock sin
        // configurar devuelve null y NO debe tumbar la edición del detalle.
        var definitions = await _conceptRepo.GetAllAsync(_tenant.TenantId)
            ?? new List<PayrollConceptDefinition>();
        string NameOf(string code, string fallback) =>
            definitions.FirstOrDefault(d => d.Code == code)?.Name ?? fallback;

        detail.Concepts ??= new List<PayrollDetailConcept>();

        // Ingresos variables: escalar = suma de sus conceptos (autorrepara filas antiguas
        // generadas antes de setear CommissionsAmount/BonusesAmount/OvertimeAmount).
        detail.CommissionsAmount = detail.Concepts.Where(c => !c.IsEmployerCost && c.ConceptCode == "COMMISSION").Sum(c => c.Amount);
        detail.BonusesAmount = detail.Concepts.Where(c => !c.IsEmployerCost && c.ConceptCode == "BONUS").Sum(c => c.Amount);
        detail.OvertimeAmount = detail.Concepts.Where(c => !c.IsEmployerCost && c.ConceptCode == "OVERTIME").Sum(c => c.Amount);

        // Retenciones de ley: el concepto refleja el monto editado.
        UpsertConcept(detail, "INSS_EMP", NameOf("INSS_EMP", "INSS Empleado"), detail.InssDeduction ?? 0m);
        UpsertConcept(detail, "IR", NameOf("IR", "Impuesto sobre la Renta"), detail.IrDeduction ?? 0m);

        // Otras deducciones: la diferencia entre el total editado y sus conceptos (préstamos,
        // anticipos, embargos, …) se vuelca en un concepto ADJUSTMENT para cuadrar la colilla.
        var fixedCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "SALARY", "VACATION_PAY", "COMMISSION", "BONUS", "OVERTIME",
            "INSS_EMP", "IR", "INSS_PAT", "OTHER_PAT", "ADJUSTMENT"
        };
        var othersSum = detail.Concepts
            .Where(c => !c.IsEmployerCost && !fixedCodes.Contains(c.ConceptCode))
            .Sum(c => c.Amount);
        var delta = (detail.OtherDeductions ?? 0m) - othersSum;
        if (delta != 0m)
        {
            var current = detail.Concepts.FirstOrDefault(c => c.ConceptCode == "ADJUSTMENT" && !c.IsEmployerCost)?.Amount ?? 0m;
            UpsertConcept(detail, "ADJUSTMENT", NameOf("ADJUSTMENT", "Ajuste Manual"), current + delta);
        }
    }

    private static void UpsertConcept(PayrollDetail detail, string code, string description, decimal amount)
    {
        var concept = detail.Concepts.FirstOrDefault(c => c.ConceptCode == code && !c.IsEmployerCost);
        if (concept is null)
        {
            // No crea filas de retención sin monto (evita conceptos INSS/IR en 0 sin dato previo).
            if (amount == 0m) return;
            detail.Concepts.Add(new PayrollDetailConcept
            {
                ConceptCode = code,
                Description = description,
                Amount = amount,
                IsEmployerCost = false,
                CompanyId = detail.CompanyId,
                PayrollDetailId = detail.Id
            });
            return;
        }

        concept.Amount = amount;
        concept.Description = description;
    }

    // --- Mapping ---

    private static DeductionTypeResponse MapDeductionType(DeductionType d) => new(
        d.Id, d.Code, d.Name, d.CalculationMethod, d.Rate, d.FixedAmount, d.IsMandatory, d.IsActive, d.Priority);

    private static EmployeeSalaryResponse MapSalary(EmployeeSalary s) => new(
        s.Id, s.EmployeeId, s.Employee?.FirstName + " " + s.Employee?.LastName, s.BaseSalary, s.SalaryType,
        s.EffectiveDate, s.EndDate, s.IsActive, s.Notes);

    public async Task<AchExportResult?> ExportAchFileAsync(Guid runId)
    {
        var run = await _repo.GetRunByIdAsync(runId);
        if (run is null || run.Status != "approved") return null;

        var fileContent = await _achService.GenerateAchFileAsync(runId);
        return new AchExportResult(fileContent, _achService.FileName);
    }

    private static PayrollPeriodResponse MapPeriod(PayrollPeriod p) => new(
        p.Id, p.Name, p.Year, p.Month, p.PeriodNumber, p.StartDate, p.EndDate, p.PaymentDate, p.Status);

    private static PayrollRunResponse MapRun(PayrollRun r) => new(
        r.Id, r.PayrollPeriodId, r.PayrollPeriod?.Name ?? "", r.Status, r.TotalSalaries, r.TotalDeductions,
        r.TotalNetPay, r.EmployeeCount, r.ProcessedAt, r.Notes,
        r.Details?.Select(d => new PayrollDetailResponse(
            d.Id, d.EmployeeId, d.Employee?.FirstName + " " + d.Employee?.LastName ?? "",
            d.Employee?.EmployeeCode ?? "", d.BaseSalary, d.GrossPay, d.TotalDeductions, d.NetPay,
            d.InssCode, d.InssDeduction, d.IrDeduction, d.OtherDeductions, d.PaymentStatus, d.Currency,
            d.Concepts?.Select(c => new PayrollDetailConceptResponse(c.ConceptCode, c.Description, c.Amount, c.IsEmployerCost)).ToList())).ToList() ?? []);
}
