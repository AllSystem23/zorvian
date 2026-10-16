using Moq;
using Zorvian.Application.DTOs.Permission;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Permisos → saldo de LeaveBalances + medio día:
/// - Aprobar descuenta SickDaysTaken/PersonalDaysTaken según el código del tipo (recálculo
///   idempotente desde los permisos aprobados — patrón LeaveBalanceSync, sin "+=").
/// - Tipos auto-aprobados (RequiresApproval = false) descuentan sus buckets al crearse.
/// - isHalfDay: TotalDays/BusinessDays = 0.5 con fecha única; fechas distintas → error.
/// Solo los tipos sembrados mapean bucket; los tipos custom de la compañía no tocan saldos.
/// </summary>
public sealed class PermissionLeaveTests
{
    private readonly Mock<IPermissionRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<IVacationRepository> _vacationRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IJobScheduler> _jobScheduler = new();
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly PermissionService _sut;
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly string _tenantId = Guid.NewGuid().ToString();

    public PermissionLeaveTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(_employeeId);
        _storage.Setup(s => s.GetSignedUrlAsync(It.IsAny<string?>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync((string? v, TimeSpan? _) => v);
        _sut = new PermissionService(_repo.Object, _employeeRepo.Object, _vacationRepo.Object,
            _tenant.Object, _notification.Object, _jobScheduler.Object, _storage.Object);
    }

    // ── Helpers ──

    private static LeaveType MakeType(string code, bool requiresApproval = true, bool isPaid = true) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code,
        IsPaid = isPaid,
        RequiresApproval = requiresApproval,
        RequiresAttachment = false,
    };

    private PermissionRequest MakeRequest(LeaveType type, string status = "pending") => new()
    {
        Id = Guid.NewGuid(),
        EmployeeId = _employeeId,
        Employee = new Employee { Id = _employeeId, FirstName = "Ana", LastName = "Test", EmployeeCode = "E-1" },
        LeaveTypeId = type.Id,
        LeaveType = type,
        StartDate = new DateOnly(2026, 9, 7),
        EndDate = new DateOnly(2026, 9, 9),
        TotalDays = 3,
        BusinessDays = 3,
        Status = status,
    };

    /// <summary>Sumas de buckets por código: SICK/SUBSIDY/MEDICAL_APPT → 4; PERSONAL/STUDY → 1.5.</summary>
    private void SetupBucketSums(int year)
    {
        _repo.Setup(r => r.GetApprovedDaysByCodesAsync(_employeeId, year,
                It.Is<List<string>>(c => c.Contains("SICK"))))
            .ReturnsAsync(4m);
        _repo.Setup(r => r.GetApprovedDaysByCodesAsync(_employeeId, year,
                It.Is<List<string>>(c => c.Contains("PERSONAL") && !c.Contains("SICK"))))
            .ReturnsAsync(1.5m);
    }

    private void ArrangeCreate(LeaveType type, LeaveBalances? existingBalance = null)
    {
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId))
            .ReturnsAsync(new Employee
            {
                Id = _employeeId,
                FirstName = "Ana",
                LastName = "Test",
                EmployeeCode = "E-1",
                HireDate = new DateOnly(2025, 1, 1),
            });
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(type.Id)).ReturnsAsync(type);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(_employeeId, It.IsAny<int>()))
            .ReturnsAsync(existingBalance);
    }

    // ── Aprobación → buckets ──

    [Fact]
    public async Task ApproveAsync_RecalculatesSickAndPersonalBuckets()
    {
        var type = MakeType("SICK");
        var perm = MakeRequest(type);
        var balance = new LeaveBalances { Id = Guid.NewGuid(), EmployeeId = _employeeId, Year = 2026 };
        _repo.Setup(r => r.GetByIdAsync(perm.Id)).ReturnsAsync(perm);
        SetupBucketSums(perm.StartDate.Year);
        _vacationRepo.Setup(r => r.GetLeaveBalanceAsync(_employeeId, perm.StartDate.Year)).ReturnsAsync(balance);

        var result = await _sut.ApproveAsync(perm.Id, "ok");

        Assert.NotNull(result);
        Assert.Equal("approved", result.Status);
        // Escritura por REESCRITURA desde los permisos aprobados (idempotente), no "antiguo + n".
        Assert.Equal(4m, balance.SickDaysTaken);
        Assert.Equal(1.5m, balance.PersonalDaysTaken);
        _vacationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_RejectedNotCalled_WhenStatusNotPending()
    {
        var type = MakeType("SICK");
        var perm = MakeRequest(type, status: "approved");
        _repo.Setup(r => r.GetByIdAsync(perm.Id)).ReturnsAsync(perm);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveAsync(perm.Id, "ok"));

        _repo.Verify(r => r.GetApprovedDaysByCodesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<string>>()),
            Times.Never);
        _vacationRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // ── Tipo auto-aprobado → buckets al crearse ──

    [Fact]
    public async Task CreateAsync_AutoApprovedType_SyncsBucketsOnCreate()
    {
        var type = MakeType("MATERNITY", requiresApproval: false, isPaid: true);
        ArrangeCreate(type);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        SetupBucketSums(date.Year);
        var result = await _sut.CreateAsync(
            new CreatePermissionRequest(type.Id, date, date, "Licencia", null, null));

        Assert.Equal("approved", result.Status);
        // Sin fila previa de saldo → se crea con los buckets ya calculados.
        _vacationRepo.Verify(
            r => r.AddLeaveBalanceAsync(It.Is<LeaveBalances>(b =>
                b.EmployeeId == _employeeId && b.SickDaysTaken == 4m && b.PersonalDaysTaken == 1.5m)),
            Times.Once);
        _vacationRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PendingType_DoesNotTouchBuckets()
    {
        var type = MakeType("SICK", requiresApproval: true);
        ArrangeCreate(type);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var result = await _sut.CreateAsync(
            new CreatePermissionRequest(type.Id, date, date, "Cita", null, null));

        Assert.Equal("pending", result.Status);
        _vacationRepo.Verify(r => r.GetLeaveBalanceAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        _repo.Verify(r => r.GetApprovedDaysByCodesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<List<string>>()),
            Times.Never);
    }

    // ── Medio día ──

    [Fact]
    public async Task CreateAsync_HalfDay_CalculatesHalfDayAndStoresFlag()
    {
        var type = MakeType("MEDICAL_APPT");
        ArrangeCreate(type);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var result = await _sut.CreateAsync(
            new CreatePermissionRequest(type.Id, date, date, "Cita médica", null, null, IsHalfDay: true));

        Assert.Equal(0.5m, result.TotalDays);
        Assert.Equal(0.5m, result.BusinessDays);
        Assert.True(result.IsHalfDay);
        Assert.Equal(date, result.StartDate);
        Assert.Equal(date, result.EndDate);
    }

    [Fact]
    public async Task CreateAsync_HalfDay_DifferentDates_Throws()
    {
        var type = MakeType("MEDICAL_APPT");
        ArrangeCreate(type);

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.CreateAsync(new CreatePermissionRequest(
                type.Id, date, date.AddDays(1), "Cita médica", null, null, IsHalfDay: true)));

        Assert.Contains("half-day", ex.Message);
    }
}
