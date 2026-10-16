using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Base de retenciones de ley (INSS/IR):
/// - Default "taxable_gross": remuneración normal gravable (salario + variables; EXCLUYE vacaciones).
/// - "base_salary" (opt-in por CompanySettings): solo salario base.
/// - Employee.DeductInss = false (ej. pensionado del INSS en NIC): sin cuota trabajador ni patronal,
///   IR sobre base completa y sin conceptos INSS en la colilla.
/// - UpdateDetailAsync sincroniza los conceptos (la colilla imprime concepts, no los escalares).
/// </summary>
public sealed class PayrollRetentionBaseTests
{
    private readonly Mock<IPayrollRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<IAchExportService> _ach = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<ICountryTaxConfigRepository> _taxConfigRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IOvertimeRecordRepository> _overtimeRepo = new();
    private readonly Mock<ICommissionRecordRepository> _commissionRepo = new();
    private readonly Mock<IBonusRecordRepository> _bonusRepo = new();
    private readonly Mock<IPayrollConceptRepository> _conceptRepo = new();
    private readonly Mock<IEmployeeLoanRepository> _loanRepo = new();
    private readonly Mock<ISalaryAdvanceRepository> _advanceRepo = new();
    private readonly Mock<IWageGarnishmentRepository> _garnishmentRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _benefitProvisionRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IPayrollCalculationStrategy> _strategy = new();
    private readonly Mock<IBankTransferService> _bankTransfer = new();
    private readonly Mock<ISickLeaveRepository> _sickLeaveRepo = new();
    private readonly Mock<IVacationRepository> _vacationRepo = new();
    private readonly Mock<IPermissionRepository> _permissionRepo = new();
    private readonly PayrollService _sut;

    private static readonly DateOnly PeriodStart = new(2026, 6, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 6, 30);

    public PayrollRetentionBaseTests()
    {
        _strategy.Setup(s => s.CountryCode).Returns("NIC");
        var factory = new PayrollCalculationFactory(new List<IPayrollCalculationStrategy> { _strategy.Object });

        _sut = new PayrollService(
            _repo.Object, _employeeRepo.Object, _tenant.Object, _webhook.Object, _ach.Object,
            _autoAccounting.Object, _taxConfigRepo.Object, _companyRepo.Object,
            _overtimeRepo.Object, _commissionRepo.Object, _bonusRepo.Object, _conceptRepo.Object,
            _loanRepo.Object, _advanceRepo.Object, _garnishmentRepo.Object, _benefitProvisionRepo.Object,
            _auditRepo.Object, factory, _bankTransfer.Object, _sickLeaveRepo.Object,
            _vacationRepo.Object, _permissionRepo.Object);
    }

    // ── Helpers ──

    private (Guid PeriodId, Guid CompanyId, Guid EmpId) ArrangeGenerate(
        Employee emp,
        CompanySettings? settings,
        List<VacationRequest>? vacations = null,
        List<AttendanceRecord>? attendance = null)
    {
        var periodId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var period = new PayrollPeriod { Id = periodId, StartDate = PeriodStart, EndDate = PeriodEnd };

        _tenant.Setup(t => t.TenantId).Returns(companyId.ToString());
        _repo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _companyRepo.Setup(r => r.GetByTenantIdAsync(companyId.ToString()))
            .ReturnsAsync(new Company { Id = companyId, Country = "NIC" });
        _companyRepo.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>())).ReturnsAsync(settings);
        _taxConfigRepo.Setup(r => r.GetByCountryCodeAsync("NIC"))
            .ReturnsAsync(new CountryTaxConfig
            {
                InssEmployeeRate = 0.07m,
                InssEmployeeMax = 1500m,
                InssEmployerRate = 0.215m,
                InssEmployerMax = 5000m,
                ChristmasBonusPercentage = 0.0833m
            });

        _employeeRepo.Setup(r => r.GetFilteredAsync(null, "active", null, 1, 10000, It.IsAny<Guid?>()))
            .ReturnsAsync(new List<Employee> { emp });

        var fullAttendance = attendance ?? Enumerable.Range(1, 30)
            .Select(i => new AttendanceRecord { Date = new DateOnly(2026, 6, i), Status = "present" })
            .ToList();
        _employeeRepo.Setup(r => r.GetAttendanceInRangeAsync(emp.Id, PeriodStart, PeriodEnd))
            .ReturnsAsync(fullAttendance);
        _employeeRepo.Setup(r => r.GetVacationsInRangeAsync(emp.Id, PeriodStart, PeriodEnd))
            .ReturnsAsync(vacations ?? new List<VacationRequest>());

        _overtimeRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<OvertimeRecord>());
        _commissionRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<CommissionRecord>());
        _bonusRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId)).ReturnsAsync(new List<BonusRecord>());
        _conceptRepo.Setup(r => r.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<PayrollConceptDefinition>());
        _loanRepo.Setup(r => r.GetActiveLoansAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<EmployeeLoan>());
        _advanceRepo.Setup(r => r.GetByEmployeeAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<SalaryAdvance>());
        _garnishmentRepo.Setup(r => r.GetActiveGarnishmentsAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new List<WageGarnishment>());
        _benefitProvisionRepo.Setup(r => r.AddAsync(It.IsAny<BenefitProvision>())).Returns(Task.CompletedTask);
        _benefitProvisionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddRunAsync(It.IsAny<PayrollRun>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.AddDetailsAsync(It.IsAny<List<PayrollDetail>>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        return (periodId, companyId, emp.Id);
    }

    private async Task<List<PayrollDetail>> GenerateAndGetDetailsAsync(Guid periodId)
    {
        List<PayrollDetail>? captured = null;
        _repo.Setup(r => r.AddDetailsAsync(It.IsAny<List<PayrollDetail>>()))
            .Callback<List<PayrollDetail>>(l => captured = l)
            .Returns(Task.CompletedTask);

        await _sut.GeneratePayrollAsync(new GeneratePayrollRequest(periodId, null));
        return captured ?? throw new InvalidOperationException("AddDetailsAsync no fue invocado");
    }

    private PayrollDetail ArrangeDetailWithConcepts(params (string Code, string Desc, decimal Amount, bool Employer)[] concepts)
    {
        var detail = new PayrollDetail
        {
            Id = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            BaseSalary = 3000m,
            GrossPay = 3350m,
            InssDeduction = 234.50m,
            IrDeduction = 0m,
            OtherDeductions = 0m,
            TotalDeductions = 234.50m,
            NetPay = 3115.50m,
            Currency = "NIO"
        };
        foreach (var c in concepts)
        {
            detail.Concepts.Add(new PayrollDetailConcept
            {
                ConceptCode = c.Code,
                Description = c.Desc,
                Amount = c.Amount,
                IsEmployerCost = c.Employer,
                PayrollDetailId = detail.Id
            });
        }
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetDetailByIdAsync(detail.Id)).ReturnsAsync(detail);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _conceptRepo.Setup(r => r.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<PayrollConceptDefinition>());
        return detail;
    }

    // ── GeneratePayroll: base de retención ──

    [Fact]
    public async Task GeneratePayroll_TaxableGross_ExcludesVacationAndSetsVariableScalars()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, companyId, _) = ArrangeGenerate(emp, settings: null,
            vacations: new List<VacationRequest> { new() { Status = "approved", BusinessDays = 3 } });

        // Variable concepts: overtime 100 + commission 200 + bonus 50
        _overtimeRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<OvertimeRecord> { new() { EmployeeId = emp.Id, Amount = 100m } });
        _commissionRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<CommissionRecord> { new() { EmployeeId = emp.Id, Amount = 200m } });
        _bonusRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<BonusRecord> { new() { EmployeeId = emp.Id, Amount = 50m } });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // Gross = 3000 + 100 + 200 + 50 + 300 (vacaciones: 3 días × 3000/30)
        Assert.Equal(3650m, d.GrossPay);
        // Base gravable = gross − vacaciones = 3350 → INSS 7% = 234.50 (vacaciones NO cotizan)
        Assert.Equal(234.50m, d.InssDeduction);
        // Escalares de variables que la Colilla imprime (antes salían 0.00)
        Assert.Equal(200m, d.CommissionsAmount);
        Assert.Equal(50m, d.BonusesAmount);
        Assert.Equal(100m, d.OvertimeAmount);
        // El IR se calcula sobre la misma base gravable (excluyendo vacaciones)
        _strategy.Verify(s => s.CalculateIr(3350m, 234.50m, It.IsAny<CountryTaxConfig>()), Times.Once);
    }

    [Fact]
    public async Task GeneratePayroll_RetentionBaseSalarySetting_RetainsOnlyOnBaseSalary()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, _, _) = ArrangeGenerate(emp,
            settings: new CompanySettings { RetentionBase = "base_salary" });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // Opt-in "base_salary": INSS solo sobre el salario base (3000 × 7% = 210)
        Assert.Equal(210m, d.InssDeduction);
    }

    [Fact]
    public async Task GeneratePayroll_DeductInssFalse_NoInssConceptsAndIrOnFullBase()
    {
        var emp = new Employee
        {
            Id = Guid.NewGuid(),
            Salary = 3000m,
            SalaryType = "monthly",
            DeductInss = false // pensionado del INSS: no cotiza
        };
        var (periodId, companyId, _) = ArrangeGenerate(emp, settings: null);
        _overtimeRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<OvertimeRecord> { new() { EmployeeId = emp.Id, Amount = 100m } });
        _commissionRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<CommissionRecord> { new() { EmployeeId = emp.Id, Amount = 200m } });
        _bonusRepo.Setup(r => r.GetByPeriodAsync(periodId, companyId))
            .ReturnsAsync(new List<BonusRecord> { new() { EmployeeId = emp.Id, Amount = 50m } });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // Gross = 3000 + 100 + 200 + 50 = 3350
        Assert.Equal(3350m, d.GrossPay);
        // Sin cuota del trabajador ni patronal
        Assert.Equal(0m, d.InssDeduction);
        Assert.DoesNotContain(d.Concepts, c => c.ConceptCode == "INSS_EMP");
        Assert.DoesNotContain(d.Concepts, c => c.ConceptCode == "INSS_PAT");
        // El IR se calcula con INSS = 0 → base gravable completa
        _strategy.Verify(s => s.CalculateIr(3350m, 0m, It.IsAny<CountryTaxConfig>()), Times.Once);
        // El concepto IR sí existe (colilla con fila IR)
        Assert.Contains(d.Concepts, c => c.ConceptCode == "IR");
    }

    // ── UpdateDetailAsync: sync de conceptos (Planilla ↔ Colilla) ──

    [Fact]
    public async Task UpdateDetail_InssEdit_SyncsConceptAndRecalculatesTotals()
    {
        var detail = ArrangeDetailWithConcepts(
            ("SALARY", "Salario Base", 3000m, false),
            ("INSS_EMP", "INSS Empleado", 234.50m, false),
            ("IR", "Impuesto sobre la Renta", 0m, false),
            ("COMMISSION", "Comisiones", 200m, false),
            ("OVERTIME", "Horas Extras", 100m, false),
            ("BONUS", "Bonos", 50m, false));

        var result = await _sut.UpdateDetailAsync(detail.Id,
            new UpdatePayrollDetailRequest(null, null, null, null, 200m, null, null, null));

        // El concepto INSS_EMP refleja el valor editado (la colilla lo imprime desde concepts)
        var inssConcept = detail.Concepts.Single(c => c.ConceptCode == "INSS_EMP");
        Assert.Equal(200m, inssConcept.Amount);
        // Totales recalculados desde los escalares
        Assert.Equal(200m, detail.TotalDeductions);
        Assert.Equal(3150m, detail.NetPay);
        // La respuesta también refleja el cambio
        Assert.NotNull(result);
        Assert.Equal(200m, result!.InssDeduction);
        Assert.Contains(result.Concepts!, c => c.Code == "INSS_EMP" && c.Amount == 200m);
    }

    [Fact]
    public async Task UpdateDetail_OtherDeductionsEdit_CreatesAdjustmentConcept()
    {
        var detail = ArrangeDetailWithConcepts(
            ("SALARY", "Salario Base", 3000m, false),
            ("INSS_EMP", "INSS Empleado", 234.50m, false),
            ("LOAN", "Préstamo", 500m, false));
        detail.OtherDeductions = 500m;
        detail.TotalDeductions = 734.50m;
        detail.NetPay = 2615.50m;

        await _sut.UpdateDetailAsync(detail.Id,
            new UpdatePayrollDetailRequest(null, null, null, null, null, null, 300m, null));

        // La diferencia (300 − 500) se vuelca en un concepto ADJUSTMENT para cuadrar la colilla
        var adjustment = detail.Concepts.Single(c => c.ConceptCode == "ADJUSTMENT");
        Assert.Equal(-200m, adjustment.Amount);
        Assert.Equal(300m, detail.OtherDeductions);
        Assert.Equal(534.50m, detail.TotalDeductions); // INSS 234.50 + IR 0 + otras 300
        Assert.Equal(2815.50m, detail.NetPay);         // 3350 − 534.50
    }

    [Fact]
    public async Task UpdateDetail_RefreshesVariableScalarsFromConcepts()
    {
        var detail = ArrangeDetailWithConcepts(
            ("SALARY", "Salario Base", 3000m, false),
            ("COMMISSION", "Comisiones", 200m, false),
            ("BONUS", "Bonos", 50m, false),
            ("OVERTIME", "Horas Extras", 100m, false));
        detail.CommissionsAmount = null;
        detail.BonusesAmount = null;
        detail.OvertimeAmount = null;

        await _sut.UpdateDetailAsync(detail.Id,
            new UpdatePayrollDetailRequest(null, null, null, null, null, 0m, null, null));

        // Autorrepara las claves escalares que la Colilla imprime en "Ingresos"
        Assert.Equal(200m, detail.CommissionsAmount);
        Assert.Equal(50m, detail.BonusesAmount);
        Assert.Equal(100m, detail.OvertimeAmount);
    }
}
