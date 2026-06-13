using System.Text.Json;
using Zorvian.Application.DTOs.Payroll;
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
        ISickLeaveRepository sickLeaveRepo)
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

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId) 
            ?? throw new KeyNotFoundException("Company not found");
        var taxConfig = await _taxConfigRepo.GetByCountryCodeAsync(company.Country ?? "NIC")
            ?? throw new KeyNotFoundException("Tax configuration not found for country");

        var activeEmployees = await _employeeRepo.GetFilteredAsync(null, "active", null, 1, 10000);
        var conceptDefinitions = await _conceptRepo.GetAllAsync(_tenant.TenantId);
        var strategy = _calcFactory.GetStrategy(company.Country ?? "NIC");

        // Fetch variable concepts once per payroll run
        var overtimeRecords = await _overtimeRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<OvertimeRecord>();
        var commissionRecords = await _commissionRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<CommissionRecord>();
        var bonusRecords = await _bonusRepo.GetByPeriodAsync(period.Id, company.Id) ?? new List<BonusRecord>();

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
            
            // Adjust base salary if not full attendance (simple logic: proportional to days)
            var adjustedBaseSalary = baseSalary;
            if (emp.SalaryType == "hourly")
            {
                var totalHours = attendanceRecords.Sum(a => a.TotalHours ?? 0m);
                adjustedBaseSalary = totalHours * (baseSalary); // baseSalary here acts as hourly rate
            }
            else if (workedDays < expectedDays && expectedDays > 0)
            {
                adjustedBaseSalary = (baseSalary / expectedDays) * workedDays;
            }

            // Vacation Payment (H-BA-03)
            var approvedVacations = await _employeeRepo.GetVacationsInRangeAsync(emp.Id, period.StartDate, period.EndDate);
            var vacationPay = approvedVacations.Where(v => v.Status == "approved").Sum(v => v.BusinessDays * (baseSalary / 30));

            var grossPay = adjustedBaseSalary + empOvertime + empCommission + empBonus + vacationPay;
            var inssDeduction = strategy.CalculateInssEmployee(grossPay, taxConfig);
            var irDeduction = strategy.CalculateIr(grossPay, inssDeduction, taxConfig);
            
            var garnishmentDeduction = garnishments.Sum(g => g.GarnishmentType == "fixed" ? g.Value : grossPay * (g.Value / 100));

            // Cálculo de costos patronales
            var employerInss = Math.Min(grossPay * taxConfig.InssEmployerRate, taxConfig.InssEmployerMax);
            var otherEmployerCosts = grossPay * taxConfig.OtherEmployerRate;
            var totalEmployerCost = employerInss + otherEmployerCosts;

            var totalEmpDeductions = inssDeduction + irDeduction + loanDeduction + advanceDeduction + garnishmentDeduction;

            var concepts = new List<PayrollDetailConcept>();
            var tenantId = Guid.Parse(_tenant.TenantId);

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
            AddConcept("INSS_EMP", inssDeduction);
            AddConcept("IR", irDeduction);
            AddConcept("INSS_PAT", employerInss, true);

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
        };

        await _repo.AddRunAsync(run);
        await _repo.SaveChangesAsync();

        foreach (var d in details)
        {
            d.PayrollRunId = run.Id;
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
                    }

                    // Generate accounting entry
                    await _autoAccounting.GeneratePayrollEntryAsync(run.Id);

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
