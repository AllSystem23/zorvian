using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Permisos en nómina + ciclo de vida de las vacaciones pagadas por planilla:
/// - Permisos aprobados CON goce (LeaveType.IsPaid) restauran salario ante ausencias de
///   asistencia (antes prorrateaban como falta y descuentaban indebidamente).
/// - Permisos SIN goce no suman → el salario se prorratea por esos días (equivale al
///   descuento "sin goce" que hacía el prorrateo natural de ausencias).
/// - Medio día (IsHalfDay) entra al prorrateo como 0.5 día.
/// - Una sola corrida no cancelada por período (evita doble pago de salarios y vacationPay).
/// - Aprobar la corrida marca las vacaciones del período como "taken" con PaidByPayrollRunId
///   y recalcula el espejo de saldo; cancelarla revierte ese consumo.
/// </summary>
public sealed class PayrollLeaveTests
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

    public PayrollLeaveTests()
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
        List<AttendanceRecord>? attendance = null,
        List<PermissionRequest>? permissions = null)
    {
        var periodId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var period = new PayrollPeriod { Id = periodId, StartDate = PeriodStart, EndDate = PeriodEnd };

        _tenant.Setup(t => t.TenantId).Returns(companyId.ToString());
        _repo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _repo.Setup(r => r.GetRunsAsync(periodId)).ReturnsAsync(new List<PayrollRun>());
        _companyRepo.Setup(r => r.GetByTenantIdAsync(companyId.ToString()))
            .ReturnsAsync(new Company { Id = companyId, Country = "NIC" });
        _companyRepo.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>())).ReturnsAsync((CompanySettings?)null);
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

        _employeeRepo.Setup(r => r.GetAttendanceInRangeAsync(emp.Id, PeriodStart, PeriodEnd))
            .ReturnsAsync(attendance ?? FullAttendance());
        _employeeRepo.Setup(r => r.GetVacationsInRangeAsync(emp.Id, PeriodStart, PeriodEnd))
            .ReturnsAsync(new List<VacationRequest>());
        _permissionRepo.Setup(r => r.GetApprovedOverlappingAsync(PeriodStart, PeriodEnd))
            .ReturnsAsync(permissions ?? new List<PermissionRequest>());

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

    private static List<AttendanceRecord> FullAttendance() =>
        Enumerable.Range(1, 30)
            .Select(i => new DateOnly(2026, 6, i))
            .Select(d => new AttendanceRecord { Date = d, Status = "present" })
            .ToList();

    private static List<AttendanceRecord> AttendanceExcluding(params DateOnly[] missing) =>
        FullAttendance().Where(a => !missing.Contains(a.Date)).ToList();

    private static PermissionRequest Permission(Guid empId, DateOnly start, DateOnly end,
        bool isPaid, bool halfDay = false)
    {
        var days = halfDay ? 0.5m : end.DayNumber - start.DayNumber + 1;
        return new PermissionRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            Status = "approved",
            StartDate = start,
            EndDate = end,
            TotalDays = days,
            BusinessDays = days,
            IsHalfDay = halfDay,
            LeaveType = new LeaveType
            {
                Id = Guid.NewGuid(),
                Code = isPaid ? "SICK" : "UNPAID",
                Name = isPaid ? "Enfermedad" : "Sin goce",
                IsPaid = isPaid,
                RequiresApproval = true,
            },
        };
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

    // ── (c) Permisos y prorrateo de asistencia ──

    [Fact]
    public async Task GeneratePayroll_PaidPermissionAbsences_DoNotProrate()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, _, _) = ArrangeGenerate(
            emp,
            // 3 días de ausencia justificada por permiso con goce (mié 10 – vie 12 jun 2026)
            attendance: AttendanceExcluding(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 11), new DateOnly(2026, 6, 12)),
            permissions: new List<PermissionRequest>
            {
                Permission(emp.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 12), isPaid: true),
            });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // 27 asistencias + 3 días con goce = 30 de 30 → salario íntegro (sin la regla serían 2700).
        Assert.Equal(3000m, d.GrossPay);
    }

    [Fact]
    public async Task GeneratePayroll_UnpaidPermission_ProratesSalary()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, _, _) = ArrangeGenerate(
            emp,
            attendance: AttendanceExcluding(new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 11), new DateOnly(2026, 6, 12)),
            permissions: new List<PermissionRequest>
            {
                Permission(emp.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 12), isPaid: false),
            });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // Sin goce: 27 de 30 días → 3000/30 × 27 = 2700 (el permiso no restaura salario).
        Assert.Equal(2700m, d.GrossPay);
    }

    [Fact]
    public async Task GeneratePayroll_HalfDayPaidPermission_ProratesHalfDay()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, _, _) = ArrangeGenerate(
            emp,
            // Lunes 15 jun 2026 ausente, permiso con goce de MEDIO día ese mismo día
            attendance: AttendanceExcluding(new DateOnly(2026, 6, 15)),
            permissions: new List<PermissionRequest>
            {
                Permission(emp.Id, new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 15), isPaid: true, halfDay: true),
            });

        var details = await GenerateAndGetDetailsAsync(periodId);
        var d = Assert.Single(details);

        // 29 asistencias + 0.5 con goce = 29.5 de 30 → 3000/30 × 29.5 = 2950.
        Assert.Equal(2950m, d.GrossPay);
    }

    // ── Una sola corrida por período ──

    [Fact]
    public async Task GeneratePayroll_ExistingRun_Throws_CancelledDoesNotBlock()
    {
        var emp = new Employee { Id = Guid.NewGuid(), Salary = 3000m, SalaryType = "monthly" };
        var (periodId, _, _) = ArrangeGenerate(emp);

        _repo.Setup(r => r.GetRunsAsync(periodId))
            .ReturnsAsync(new List<PayrollRun> { new() { Id = Guid.NewGuid(), Status = "draft", PayrollPeriodId = periodId } });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GeneratePayrollAsync(new GeneratePayrollRequest(periodId, null)));
        Assert.Contains("corrida", ex.Message);

        // Una corrida cancelada no bloquea regenerar (la cancelación reabrió el período
        // y revirtió el consumo de vacaciones).
        _repo.Setup(r => r.GetRunsAsync(periodId))
            .ReturnsAsync(new List<PayrollRun> { new() { Id = Guid.NewGuid(), Status = "cancelled", PayrollPeriodId = periodId } });

        var details = await GenerateAndGetDetailsAsync(periodId);
        Assert.Single(details);
    }

    // ── Vacaciones pagadas por planilla: flip al aprobar, revert al cancelar ──

    [Fact]
    public async Task ApproveRun_ConsumesApprovedVacationsAndSyncsBalance()
    {
        var runId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var run = new PayrollRun
        {
            Id = runId,
            Status = "pending_approval",
            TotalNetPay = 5000m,
            PayrollPeriodId = periodId,
        };
        run.ApprovalSteps.Add(new ApprovalFlow { Step = 1, Status = "pending", RequestType = "payroll" });
        var period = new PayrollPeriod { Id = periodId, StartDate = PeriodStart, EndDate = PeriodEnd };
        var vacation = new VacationRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            StartDate = new DateOnly(2026, 6, 10),
            EndDate = new DateOnly(2026, 6, 14),
            BusinessDays = 5,
            TotalDays = 5,
            Status = "approved",
        };
        var balance = new LeaveBalances { Id = Guid.NewGuid(), EmployeeId = empId, Year = 2026, VacationDaysTaken = 0m };

        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);
        _repo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _vacationRepo.Setup(r => r.GetApprovedInRangeAsync(PeriodStart, PeriodEnd))
            .ReturnsAsync(new List<VacationRequest> { vacation });
        _vacationRepo.Setup(r => r.GetVacationBalanceSumsAsync(empId, 2026))
            .ReturnsAsync(new LeaveBalanceSums(5m, 0m));
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, 2026)).ReturnsAsync(balance);

        var result = await _sut.ApproveRunAsync(runId);

        Assert.NotNull(result);
        Assert.Equal("approved", run.Status);
        // Vacaciones pagadas por ESTA corrida: consumidas una sola vez y trazables.
        Assert.Equal("taken", vacation.Status);
        Assert.Equal(runId, vacation.PaidByPayrollRunId);
        // Espejo de saldo recalculado (idempotente), no incrementado a mano.
        Assert.Equal(5m, balance.VacationDaysTaken);
        Assert.Equal("closed", period.Status);
        _autoAccounting.Verify(a => a.GeneratePayrollEntryAsync(runId, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task CancelRun_RevertsVacationConsumption()
    {
        var runId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        var run = new PayrollRun
        {
            Id = runId,
            Status = "approved",
            PayrollPeriodId = periodId,
        };
        var period = new PayrollPeriod { Id = periodId, StartDate = PeriodStart, EndDate = PeriodEnd, Status = "closed" };
        var vacation = new VacationRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            StartDate = new DateOnly(2026, 6, 10),
            EndDate = new DateOnly(2026, 6, 14),
            BusinessDays = 5,
            TotalDays = 5,
            Status = "taken",
            PaidByPayrollRunId = runId,
        };
        var balance = new LeaveBalances
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            Year = 2026,
            VacationDaysTaken = 5m,
            VacationDaysPending = 0m,
        };

        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _repo.Setup(r => r.GetRunByIdAsync(runId)).ReturnsAsync(run);
        _repo.Setup(r => r.GetPeriodByIdAsync(periodId)).ReturnsAsync(period);
        _vacationRepo.Setup(r => r.GetByPayrollRunAsync(runId)).ReturnsAsync(new List<VacationRequest> { vacation });
        _vacationRepo.Setup(r => r.GetVacationBalanceSumsAsync(empId, 2026))
            .ReturnsAsync(new LeaveBalanceSums(0m, 5m));
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(empId, 2026)).ReturnsAsync(balance);

        var result = await _sut.CancelRunAsync(runId);

        Assert.NotNull(result);
        Assert.Equal("cancelled", run.Status);
        // Revertidas: vuelven a "approved" (disponibles para recibo RV u otra corrida)…
        Assert.Equal("approved", vacation.Status);
        Assert.Null(vacation.PaidByPayrollRunId);
        // …y el espejo libera los 5 días consumidos (ahora reservados como aprobadas).
        Assert.Equal(0m, balance.VacationDaysTaken);
        Assert.Equal(5m, balance.VacationDaysPending);
        // El período se reabre para poder regenerar.
        Assert.Equal("open", period.Status);
    }
}
