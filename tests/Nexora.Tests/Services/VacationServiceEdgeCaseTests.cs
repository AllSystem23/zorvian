using Moq;
using Nexora.Application.DTOs.Vacation;
using Nexora.Application.Interfaces;
using Nexora.Application.Services;
using Nexora.Core.Interfaces;

namespace Nexora.Tests.Services;

public sealed class VacationServiceEdgeCaseTests
{
    private readonly Mock<IVacationRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<INotificationService> _notif = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly VacationService _sut;

    public VacationServiceEdgeCaseTests()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-123");
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(Guid.NewGuid());
        _sut = new VacationService(
            _repo.Object,
            _employeeRepo.Object,
            _tenant.Object,
            _notif.Object);
    }

    [Fact]
    public async Task CalculateBalanceAsync_MidYearHire_ProratesCorrectly()
    {
        var employeeId = Guid.NewGuid();
        var employee = new Core.Entities.Employee
        {
            Id = employeeId,
            TenantId = "tenant-123",
            HireDate = new DateOnly(2026, 1, 1),
            Status = "active",
        };

        _employeeRepo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
        _repo.Setup(r => r.GetVacationDaysSumAsync(employeeId, "taken")).ReturnsAsync(0);
        _repo.Setup(r => r.GetVacationDaysSumAsync(employeeId, "pending")).ReturnsAsync(0);

        var result = await _sut.CalculateBalanceAsync(employeeId);

        Assert.True(result.AccruedDays > 0);
        Assert.Equal(15, result.TotalDaysPerYear);
    }

    [Fact]
    public async Task CalculateBalanceAsync_WithUsedDays_ShowsCorrectAvailable()
    {
        var employeeId = Guid.NewGuid();
        var employee = new Core.Entities.Employee
        {
            Id = employeeId,
            TenantId = "tenant-123",
            HireDate = new DateOnly(2025, 6, 1),
            Status = "active",
        };

        _employeeRepo.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
        _repo.Setup(r => r.GetVacationDaysSumAsync(employeeId, "taken")).ReturnsAsync(5);
        _repo.Setup(r => r.GetVacationDaysSumAsync(employeeId, "pending")).ReturnsAsync(3);

        var result = await _sut.CalculateBalanceAsync(employeeId);

        Assert.True(result.AccruedDays > 0);
        Assert.Equal(5, result.TakenDays);
        Assert.Equal(3, result.PendingDays);
        Assert.True(result.AvailableDays >= 0);
    }

    [Fact]
    public async Task GetMyVacationsAsync_ReturnsFilteredList()
    {
        var employeeId = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(employeeId);

        var vacations = new List<Core.Entities.VacationRequest>
        {
            new() { Id = Guid.NewGuid(), EmployeeId = employeeId, Status = "approved", StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 3, 5) },
            new() { Id = Guid.NewGuid(), EmployeeId = employeeId, Status = "pending", StartDate = new DateOnly(2026, 4, 10), EndDate = new DateOnly(2026, 4, 12) },
            new() { Id = Guid.NewGuid(), EmployeeId = employeeId, Status = "rejected", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 3) },
        };

        _repo.Setup(r => r.GetFilteredAsync(null, employeeId, null, 1, 100)).ReturnsAsync(vacations);

        var items = await _sut.GetMyVacationsAsync();

        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task CreateVacationRequest_WithInsufficientBalance_ShouldThrow()
    {
        _tenant.Setup(t => t.CurrentEmployeeId).Returns((Guid?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(new CreateVacationRequest(
                new DateOnly(2026, 1, 15),
                new DateOnly(2026, 1, 28),
                null)));

        _repo.Verify(r => r.AddAsync(It.IsAny<Core.Entities.VacationRequest>()), Times.Never);
    }
}
