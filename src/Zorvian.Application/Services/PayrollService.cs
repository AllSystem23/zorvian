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

    public PayrollService(
        IPayrollRepository repo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        IWebhookService webhookService,
        IAchExportService achService)
    {
        _repo = repo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _webhookService = webhookService;
        _achService = achService;
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

    public async Task<PayrollRunResponse> GeneratePayrollAsync(GeneratePayrollRequest request)
    {
        var period = await _repo.GetPeriodByIdAsync(request.PayrollPeriodId)
            ?? throw new InvalidOperationException("Payroll period not found");

        var activeEmployees = await _employeeRepo.GetFilteredAsync(null, "active", null, 1, 10000);

        var totalSalaries = 0m;
        var totalDeductions = 0m;
        var details = new List<PayrollDetail>();

        foreach (var emp in activeEmployees)
        {
            var salary = await _repo.GetActiveSalaryAsync(emp.Id);
            var baseSalary = salary?.BaseSalary ?? emp.Salary ?? 0;

            var grossPay = baseSalary;
            var inssDeduction = CalculateInss(grossPay);
            var irDeduction = CalculateIr(grossPay - inssDeduction);
            var otherDeductions = 0m;
            var totalEmpDeductions = inssDeduction + irDeduction + otherDeductions;

            details.Add(new PayrollDetail
            {
                EmployeeId = emp.Id,
                BaseSalary = baseSalary,
                GrossPay = grossPay,
                TotalDeductions = totalEmpDeductions,
                NetPay = grossPay - totalEmpDeductions,
                InssCode = emp.IdentificationNumber,
                InssDeduction = inssDeduction,
                IrDeduction = irDeduction,
                OtherDeductions = otherDeductions,
                Details = "{}",
            });

            totalSalaries += grossPay;
            totalDeductions += totalEmpDeductions;
        }

        var run = new PayrollRun
        {
            PayrollPeriodId = request.PayrollPeriodId,
            Status = "draft",
            TotalSalaries = totalSalaries,
            TotalDeductions = totalDeductions,
            TotalNetPay = totalSalaries - totalDeductions,
            EmployeeCount = details.Count,
            Notes = request.Notes,
        };

        await _repo.AddRunAsync(run);
        await _repo.SaveChangesAsync();

        foreach (var d in details)
        {
            d.PayrollRunId = run.Id;
        }
        await _repo.AddDetailsAsync(details);
        await _repo.SaveChangesAsync();

        return MapRun(run);
    }

    public async Task<PayrollRunResponse?> ApproveRunAsync(Guid id)
    {
        var run = await _repo.GetRunByIdAsync(id);
        if (run is null) return null;
        run.Status = "approved";
        run.ProcessedAt = DateTime.UtcNow;
        run.ProcessedBy = _tenant.CurrentEmployeeId?.ToString() ?? "system";

        var period = await _repo.GetPeriodByIdAsync(run.PayrollPeriodId);
        if (period != null)
        {
            period.Status = "closed";
            await _repo.UpdatePeriodAsync(period);
        }

        await _repo.UpdateRunAsync(run);
        await _repo.SaveChangesAsync();

        // Notify via Webhook
        await _webhookService.PublishAsync(run.TenantId, "payroll.approved", new { RunId = run.Id, TotalNetPay = run.TotalNetPay });

        return MapRun(run);
    }

    private static decimal CalculateInss(decimal grossPay)
    {
        // INSS Laboral: 7% del salario, tope máximo C$1,500
        var inss = grossPay * 0.07m;
        return Math.Min(inss, 1500m);
    }

    private static decimal CalculateIr(decimal taxableIncome)
    {
        // IR progresivo simplificado (Nicaragua)
        if (taxableIncome <= 10000m) return 0m;
        if (taxableIncome <= 20000m) return (taxableIncome - 10000m) * 0.15m;
        if (taxableIncome <= 30000m) return 1500m + (taxableIncome - 20000m) * 0.20m;
        return 3500m + (taxableIncome - 30000m) * 0.25m;
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
            d.InssCode, d.InssDeduction, d.IrDeduction, d.OtherDeductions)).ToList() ?? []);
}
