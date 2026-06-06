using Moq;
using Zorvian.Application.DTOs.Vacation;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class VacationServiceTests
{
    private readonly Mock<IVacationRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly VacationService _sut;
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public VacationServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(_employeeId);
        _sut = new VacationService(_repo.Object, _employeeRepo.Object, _tenant.Object, _notification.Object);
    }

    private Employee MakeEmployee(Guid? deptId = null, DateOnly? hireDate = null) => new()
    {
        Id = _employeeId,
        FirstName = "Juan",
        LastName = "Pérez",
        Email = "juan@test.com",
        EmployeeCode = "EMP-001",
        HireDate = hireDate ?? new DateOnly(2025, 1, 1),
        DepartmentId = deptId,
        Department = deptId is null ? null : new Department { Id = deptId.Value, Name = "Test" },
    };

    private VacationRequest MakeVacation(string status = "pending") => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = _employeeId,
        Employee = new Employee { Id = _employeeId, FirstName = "Juan", LastName = "Pérez", EmployeeCode = "EMP-001" },
        StartDate = new DateOnly(2026, 7, 1),
        EndDate = new DateOnly(2026, 7, 5),
        TotalDays = 5,
        BusinessDays = 5,
        Status = status,
        ApprovalSteps = new List<ApprovalFlow>
        {
            new() { Step = 1, ApproverId = _employeeId, Status = "pending" },
            new() { Step = 2, ApproverId = null, Status = "pending" },
        },
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesVacation()
    {
        var emp = MakeEmployee(deptId: Guid.NewGuid(), hireDate: new DateOnly(2024, 1, 1));
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _employeeRepo.Setup(r => r.GetSupervisorsAsync(_employeeId)).ReturnsAsync(new List<EmployeeSupervisor>
        {
            new() { SupervisorId = Guid.NewGuid(), Supervisor = new Employee { Id = Guid.NewGuid(), FirstName = "Jefe", LastName = "Uno" } },
        });
        _employeeRepo.Setup(r => r.GetFilteredCountAsync(null, null, emp.DepartmentId)).ReturnsAsync(10);
        _repo.Setup(r => r.GetOverlappingCountAsync(emp.DepartmentId!.Value, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), _employeeId)).ReturnsAsync(1);

        var request = new CreateVacationRequest(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 5),
            null
        );

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalDays);
        _repo.Verify(r => r.AddAsync(It.IsAny<VacationRequest>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WithPastDate_Throws()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(MakeEmployee());

        var request = new CreateVacationRequest(
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 1, 5),
            null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithEndBeforeStart_Throws()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(MakeEmployee());

        var request = new CreateVacationRequest(
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 5),
            null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithInsufficientBalance_Throws()
    {
        var emp = MakeEmployee(hireDate: new DateOnly(2026, 5, 1));
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _employeeRepo.Setup(r => r.GetSupervisorsAsync(_employeeId)).ReturnsAsync(new List<EmployeeSupervisor>());

        var request = new CreateVacationRequest(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithTeamOverlap_Throws()
    {
        var deptId = Guid.NewGuid();
        var emp = MakeEmployee(deptId: deptId);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _employeeRepo.Setup(r => r.GetSupervisorsAsync(_employeeId)).ReturnsAsync(new List<EmployeeSupervisor>());
        _employeeRepo.Setup(r => r.GetFilteredCountAsync(null, null, deptId)).ReturnsAsync(3);
        _repo.Setup(r => r.GetOverlappingCountAsync(deptId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), _employeeId)).ReturnsAsync(1);

        var request = new CreateVacationRequest(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 5),
            null
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsVacation_WhenFound()
    {
        var v = MakeVacation();
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        var result = await _sut.GetByIdAsync(v.Id);

        Assert.NotNull(result);
        Assert.Equal(v.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VacationRequest?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsPagedResults()
    {
        var items = new List<VacationRequest> { MakeVacation() };
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, 1, 20)).ReturnsAsync(items);
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null)).ReturnsAsync(1);

        var result = await _sut.GetFilteredAsync(new VacationFilterRequest(null, null, null, 1, 20));

        Assert.Single(result.Items);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task ApproveAsync_ApprovesStep_WhenPending()
    {
        var v = MakeVacation();
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        var result = await _sut.ApproveAsync(v.Id, null);

        Assert.NotNull(result);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_Throws_WhenNotPending()
    {
        var v = MakeVacation(status: "approved");
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveAsync(v.Id, null));
    }

    [Fact]
    public async Task RejectAsync_RejectsRequest()
    {
        var v = MakeVacation();
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        var result = await _sut.RejectAsync(v.Id, "No cumple requisitos");

        Assert.NotNull(result);
        Assert.Equal("rejected", result.Status);
        Assert.Equal("No cumple requisitos", result.RejectionReason);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_Throws_WhenNotPending()
    {
        var v = MakeVacation(status: "approved");
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RejectAsync(v.Id, "motivo"));
    }

    [Fact]
    public async Task ApproveAsync_SendsNotification()
    {
        var v = MakeVacation();
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        await _sut.ApproveAsync(v.Id, null);

        _notification.Verify(n => n.NotifyTenantAsync(
            _tenantId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_SendsNotification()
    {
        var v = MakeVacation();
        _repo.Setup(r => r.GetByIdAsync(v.Id)).ReturnsAsync(v);

        await _sut.RejectAsync(v.Id, "Motivo de prueba");

        _notification.Verify(n => n.NotifyTenantAsync(
            _tenantId,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CalculateBalanceAsync_ReturnsCorrectBalance()
    {
        var emp = MakeEmployee(hireDate: new DateOnly(2025, 1, 1));
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetVacationDaysSumAsync(_employeeId, "taken")).ReturnsAsync(3);
        _repo.Setup(r => r.GetVacationDaysSumAsync(_employeeId, "pending")).ReturnsAsync(2);

        var result = await _sut.CalculateBalanceAsync();

        Assert.Equal(15, result.TotalDaysPerYear);
        Assert.Equal(3, result.TakenDays);
        Assert.Equal(2, result.PendingDays);
    }

    [Fact]
    public async Task CalculateBalanceAsync_Throws_WhenEmployeeNotFound()
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Employee?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CalculateBalanceAsync());
    }
}
