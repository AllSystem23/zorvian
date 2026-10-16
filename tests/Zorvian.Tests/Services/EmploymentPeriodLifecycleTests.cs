using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

/// <summary>
/// Historial de contratos: aplicar liquidación (termina empleado + cierra el
/// período abierto + desactiva salario + liquida saldos de vacaciones del año)
/// y recontratación (nuevo período sobre el MISMO empleado con antigüedad
/// reiniciada, salario nuevo y balances en cero).
/// </summary>
public sealed class EmploymentPeriodLifecycleTests
{
    private readonly Mock<ITerminationRepository> _termRepo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _benefitRepo = new();
    private readonly Mock<ICountryTaxConfigRepository> _taxConfigRepo = new();
    private readonly Mock<IVacationRepository> _vacationRepo = new();
    private readonly Mock<IPayrollRepository> _payrollRepo = new();
    private readonly Mock<IEmploymentPeriodRepository> _periodRepo = new();
    private readonly TerminationService _sut;

    private readonly Guid _employeeId = Guid.NewGuid();

    public EmploymentPeriodLifecycleTests()
    {
        _sut = new TerminationService(
            _termRepo.Object, _employeeRepo.Object, _benefitRepo.Object,
            _taxConfigRepo.Object, _vacationRepo.Object, _payrollRepo.Object,
            _periodRepo.Object);

        // Defaults: sin salary activo ni balance (los tests que los necesitan los stubbean).
        _payrollRepo.Setup(r => r.GetActiveSalaryAsync(It.IsAny<Guid>()))
            .ReturnsAsync((EmployeeSalary?)null);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync((LeaveBalances?)null);
    }

    private Employee MakeEmployee(string status = "active") => new()
    {
        Id = _employeeId,
        FirstName = "Ana",
        LastName = "López",
        Email = "ana@test.com",
        HireDate = new DateOnly(2025, 1, 15),
        Salary = 15000m,
        Status = status,
        CountryCode = "NIC",
    };

    private TerminationRecord MakeRecord(string status = "draft") => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = _employeeId,
        TerminationDate = new DateOnly(2026, 6, 30),
        Reason = TerminationReason.VoluntaryResignation,
        HireDate = new DateOnly(2025, 1, 15),
        Status = status,
    };

    // ── ApplyTerminationAsync ────────────────────────────────────────────

    [Fact]
    public async Task Apply_MarksEmployeeTerminated_ClosesPeriod_DeactivatesSalary()
    {
        var employee = MakeEmployee();
        var record = MakeRecord();
        var openPeriod = new EmploymentPeriod
        {
            Id = Guid.NewGuid(),
            EmployeeId = _employeeId,
            PeriodNumber = 1,
            HireDate = new DateOnly(2025, 1, 15),
            Status = "open",
        };
        var salary = new EmployeeSalary { Id = Guid.NewGuid(), EmployeeId = _employeeId, BaseSalary = 15000m, IsActive = true };

        _termRepo.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(employee);
        _periodRepo.Setup(r => r.GetOpenByEmployeeAsync(_employeeId)).ReturnsAsync(openPeriod);
        _payrollRepo.Setup(r => r.GetActiveSalaryAsync(_employeeId)).ReturnsAsync(salary);

        var result = await _sut.ApplyTerminationAsync(record.Id);

        // Empleado terminado + history
        employee.Status.Should().Be("terminated");
        employee.TerminationDate.Should().Be(record.TerminationDate);
        employee.TerminationReason.Should().Be("VoluntaryResignation");
        employee.History.Should().ContainSingle(h => h.ChangeType == "Terminate");

        // Período cerrado vinculado a la liquidación
        openPeriod.Status.Should().Be("closed");
        openPeriod.TerminationDate.Should().Be(record.TerminationDate);
        openPeriod.TerminationRecordId.Should().Be(record.Id);
        _periodRepo.Verify(r => r.UpdateAsync(openPeriod), Times.Once);

        // Salario desactivado
        salary.IsActive.Should().BeFalse();
        _payrollRepo.Verify(r => r.UpdateSalaryAsync(salary), Times.Once);

        // Registro procesado
        result.Status.Should().Be("processed");
        _termRepo.Verify(r => r.UpdateAsync(record), Times.Once);
    }

    [Fact]
    public async Task Apply_LiquidatesVacationBalance_AccruedEqualsTaken()
    {
        // Los días devengados no tomados ya se pagaron en la liquidación: al
        // cerrar, accrued = taken para que la recontratación arranque en cero.
        var record = MakeRecord();
        var balance = new LeaveBalances
        {
            EmployeeId = _employeeId,
            Year = 2026,
            VacationDaysAccrued = 12.5m,
            VacationDaysTaken = 5m,
            VacationDaysPending = 2m,
        };
        _termRepo.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(MakeEmployee());
        _periodRepo.Setup(r => r.GetOpenByEmployeeAsync(_employeeId)).ReturnsAsync((EmploymentPeriod?)null);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(_employeeId, 2026)).ReturnsAsync(balance);

        await _sut.ApplyTerminationAsync(record.Id);

        balance.VacationDaysAccrued.Should().Be(5m);
        balance.VacationDaysPending.Should().Be(0m);
    }

    [Fact]
    public async Task Apply_AlreadyProcessed_Throws()
    {
        _termRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(MakeRecord(status: "processed"));

        var act = async () => await _sut.ApplyTerminationAsync(Guid.NewGuid());

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*ya fue aplicada*");
    }

    [Fact]
    public async Task Apply_UnknownRecord_Throws()
    {
        _termRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((TerminationRecord?)null);

        var act = async () => await _sut.ApplyTerminationAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── RehireAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task Rehire_OpensNewPeriod_Reactivates_ResetsSeniority()
    {
        var employee = MakeEmployee(status: "terminated");
        var lastPeriod = new EmploymentPeriod
        {
            Id = Guid.NewGuid(),
            EmployeeId = _employeeId,
            PeriodNumber = 1,
            HireDate = new DateOnly(2025, 1, 15),
            TerminationDate = new DateOnly(2026, 6, 30),
            Status = "closed",
        };
        EmploymentPeriod? added = null;

        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(employee);
        _periodRepo.Setup(r => r.GetByEmployeeAsync(_employeeId)).ReturnsAsync(new List<EmploymentPeriod> { lastPeriod });
        _periodRepo.Setup(r => r.GetNextPeriodNumberAsync(_employeeId)).ReturnsAsync(2);
        _periodRepo.Setup(r => r.AddAsync(It.IsAny<EmploymentPeriod>()))
            .Callback<EmploymentPeriod>(p => added = p)
            .Returns(Task.CompletedTask);

        var newHire = new DateOnly(2026, 10, 1);
        var result = await _sut.RehireAsync(_employeeId, new RehireEmployeeRequest(newHire, Salary: 18000m));

        // Mismo empleado reactivado con antigüedad reiniciada
        result.Should().BeSameAs(employee);
        employee.Status.Should().Be("active");
        employee.HireDate.Should().Be(newHire);
        employee.TerminationDate.Should().BeNull();
        employee.TerminationReason.Should().BeNull();
        employee.Salary.Should().Be(18000m);
        employee.History.Should().ContainSingle(h => h.ChangeType == "Rehire");

        // Nuevo período #2 abierto
        added.Should().NotBeNull();
        added!.PeriodNumber.Should().Be(2);
        added.HireDate.Should().Be(newHire);
        added.BaseSalary.Should().Be(18000m);
        added.Status.Should().Be("open");

        // Salario activo del nuevo contrato
        _payrollRepo.Verify(r => r.AddSalaryAsync(
            It.Is<EmployeeSalary>(s => s.IsActive && s.BaseSalary == 18000m && s.EffectiveDate == newHire)), Times.Once);
    }

    [Fact]
    public async Task Rehire_ActiveEmployee_Throws()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(MakeEmployee(status: "active"));

        var act = async () => await _sut.RehireAsync(_employeeId, new RehireEmployeeRequest(new DateOnly(2026, 10, 1)));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*ya está activo*");
        _periodRepo.Verify(r => r.AddAsync(It.IsAny<EmploymentPeriod>()), Times.Never);
    }

    [Fact]
    public async Task Rehire_HireDateNotAfterPreviousExit_Throws()
    {
        var employee = MakeEmployee(status: "terminated");
        var lastPeriod = new EmploymentPeriod
        {
            EmployeeId = _employeeId,
            PeriodNumber = 1,
            HireDate = new DateOnly(2025, 1, 15),
            TerminationDate = new DateOnly(2026, 6, 30),
            Status = "closed",
        };
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(employee);
        _periodRepo.Setup(r => r.GetByEmployeeAsync(_employeeId)).ReturnsAsync(new List<EmploymentPeriod> { lastPeriod });

        // Igual a la fecha de salida → inválido (debe ser posterior).
        var act = async () => await _sut.RehireAsync(
            _employeeId, new RehireEmployeeRequest(new DateOnly(2026, 6, 30)));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*posterior a la salida*");
    }

    [Fact]
    public async Task Rehire_CreatesZeroedBalance_WhenMissing()
    {
        var employee = MakeEmployee(status: "terminated");
        LeaveBalances? added = null;

        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(employee);
        _periodRepo.Setup(r => r.GetByEmployeeAsync(_employeeId)).ReturnsAsync(new List<EmploymentPeriod>());
        _periodRepo.Setup(r => r.GetNextPeriodNumberAsync(_employeeId)).ReturnsAsync(2);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(_employeeId, It.IsAny<int>()))
            .ReturnsAsync((LeaveBalances?)null);
        _vacationRepo.Setup(r => r.AddLeaveBalanceAsync(It.IsAny<LeaveBalances>()))
            .Callback<LeaveBalances>(b => added = b)
            .Returns(Task.CompletedTask);

        await _sut.RehireAsync(_employeeId, new RehireEmployeeRequest(new DateOnly(2027, 3, 1)));

        added.Should().NotBeNull();
        added!.Year.Should().Be(2027);
        added.VacationDaysAccrued.Should().Be(0m);
        added.VacationDaysTaken.Should().Be(0m);
    }

    [Fact]
    public async Task Rehire_KeepsExistingBalance_WhenYearAlreadyHasOne()
    {
        var employee = MakeEmployee(status: "terminated");
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(employee);
        _periodRepo.Setup(r => r.GetByEmployeeAsync(_employeeId)).ReturnsAsync(new List<EmploymentPeriod>());
        _periodRepo.Setup(r => r.GetNextPeriodNumberAsync(_employeeId)).ReturnsAsync(2);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(_employeeId, It.IsAny<int>()))
            .ReturnsAsync(new LeaveBalances { EmployeeId = _employeeId, Year = 2026 });

        await _sut.RehireAsync(_employeeId, new RehireEmployeeRequest(new DateOnly(2026, 10, 1)));

        _vacationRepo.Verify(r => r.AddLeaveBalanceAsync(It.IsAny<LeaveBalances>()), Times.Never);
    }

    [Fact]
    public async Task Rehire_UnknownEmployee_Throws()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Employee?)null);

        var act = async () => await _sut.RehireAsync(Guid.NewGuid(), new RehireEmployeeRequest(new DateOnly(2026, 10, 1)));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
