using Moq;
using Nexora.Application.DTOs.Employee;
using Nexora.Application.DTOs.Permission;
using Nexora.Application.Interfaces;
using Nexora.Application.Services;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Tests.Services;

public sealed class PermissionServiceTests
{
    private readonly Mock<IPermissionRepository> _repo = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IJobScheduler> _jobScheduler = new();
    private readonly PermissionService _sut;
    private readonly Guid _employeeId = Guid.NewGuid();

    public PermissionServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns("tenant-123");
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(_employeeId);
        _sut = new PermissionService(_repo.Object, _employeeRepo.Object, _tenant.Object, _notification.Object, _jobScheduler.Object);
    }

    private Employee MakeEmployee() => new()
    {
        Id = _employeeId,
        FirstName = "Juan",
        LastName = "Pérez",
        Email = "juan@test.com",
        EmployeeCode = "EMP-001",
        HireDate = new DateOnly(2025, 1, 1),
        DepartmentId = Guid.NewGuid(),
    };

    private LeaveType MakeLeaveType(string code = "PERSONAL", bool reqAttachment = false, bool reqApproval = true,
        int? maxPerRequest = null, int? maxPerMonth = null, int? maxPerYear = null) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = code switch
        {
            "PERSONAL" => "Permiso personal",
            "SICK" => "Enfermedad",
            "MATERNITY" => "Maternidad",
            "UNPAID" => "Sin goce",
            _ => "Otro",
        },
        IsPaid = code != "UNPAID",
        RequiresAttachment = reqAttachment,
        RequiresApproval = reqApproval,
        MaxDaysPerRequest = maxPerRequest,
        MaxDaysPerMonth = maxPerMonth,
        MaxDaysPerYear = maxPerYear,
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPermission()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(reqAttachment: true);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 1),
            "Asunto personal",
            "http://doc.url/file.pdf", "file.pdf"
        );

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("pending", result.Status);
        Assert.Equal("Permiso personal", result.LeaveTypeName);
        _repo.Verify(r => r.AddAsync(It.IsAny<PermissionRequest>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _jobScheduler.Verify(j => j.EnqueueOcrJob(It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAttachmentRequired_ThrowsWhenNoDocument()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "SICK", reqAttachment: true);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 3),
            "Enfermo", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("supporting document", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ExceedsMaxDaysPerRequest_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "MARRIAGE", maxPerRequest: 5);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            "Boda", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("5 days", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ExceedsMaxDaysPerYear_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "UNPAID", maxPerYear: 30);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);
        _repo.Setup(r => r.GetPermissionDaysSumAsync(_employeeId, leaveType.Id, "approved", 2026)).ReturnsAsync(25);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            "Personal", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("30 days", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStart_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType();
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 10),
            new DateOnly(2026, 7, 1),
            "Mal", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("after start", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_StartDateInPast_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType();
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            "Pasado", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("tomorrow", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MaternityLeave_AutoApproved()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "MATERNITY", reqAttachment: true, reqApproval: false);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 9, 22),
            "Maternidad",
            "http://doc.url/cert.pdf", "cert.pdf"
        );

        var result = await _sut.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("approved", result.Status);
    }

    [Fact]
    public async Task GetTypesAsync_ReturnsActiveTypes()
    {
        var types = new List<LeaveType>
        {
            MakeLeaveType("PERSONAL"),
            MakeLeaveType("SICK"),
        };
        _repo.Setup(r => r.GetActiveLeaveTypesAsync()).ReturnsAsync(types);

        var result = await _sut.GetTypesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Code == "PERSONAL");
    }

    [Fact]
    public async Task ApproveAsync_ApprovesPendingRequest()
    {
        var leaveType = MakeLeaveType();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new PermissionRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = _employeeId,
                LeaveType = leaveType,
                Status = "pending",
            });

        var result = await _sut.ApproveAsync(Guid.NewGuid(), "OK");

        Assert.NotNull(result);
        Assert.Equal("approved", result.Status);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_NonPendingRequest_Throws()
    {
        var leaveType = MakeLeaveType();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new PermissionRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = _employeeId,
                LeaveType = leaveType,
                Status = "approved",
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ApproveAsync(Guid.NewGuid(), null));
        Assert.Contains("Cannot approve", ex.Message);
    }

    [Fact]
    public async Task RejectAsync_RejectsPendingRequest()
    {
        var leaveType = MakeLeaveType();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new PermissionRequest
            {
                Id = Guid.NewGuid(),
                EmployeeId = _employeeId,
                LeaveType = leaveType,
                Status = "pending",
            });

        var result = await _sut.RejectAsync(Guid.NewGuid(), "No procede");

        Assert.NotNull(result);
        Assert.Equal("rejected", result.Status);
        Assert.Equal("No procede", result.RejectionReason);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExceedsMonthlyLimit_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "PERSONAL", maxPerMonth: 2);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);
        _repo.Setup(r => r.GetMonthlyPermissionDaysAsync(_employeeId, leaveType.Id, 2026, 7)).ReturnsAsync(2m);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 7, 16),
            "Otro asunto", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("2 days", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_PaternityLeave_Exceeds5Days_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "PATERNITY");
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 10),
            "Nacimiento", null, null
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("5 business days", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_MaternityLeave_Exceeds84Days_Throws()
    {
        var emp = MakeEmployee();
        var leaveType = MakeLeaveType(code: "MATERNITY", reqAttachment: true);
        _employeeRepo.Setup(r => r.GetByIdAsync(_employeeId)).ReturnsAsync(emp);
        _repo.Setup(r => r.GetLeaveTypeByIdAsync(leaveType.Id)).ReturnsAsync(leaveType);

        var request = new CreatePermissionRequest(
            leaveType.Id,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 10, 15),
            "Maternidad",
            "http://doc.url/cert.pdf", "cert.pdf"
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        Assert.Contains("84 days", ex.Message);
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsPagedResults()
    {
        var leaveType = MakeLeaveType();
        _repo.Setup(r => r.GetFilteredAsync(null, null, null, 1, 20))
            .ReturnsAsync(new List<PermissionRequest>
            {
                new() { Id = Guid.NewGuid(), EmployeeId = _employeeId, LeaveType = leaveType, Status = "pending" },
            });
        _repo.Setup(r => r.GetFilteredCountAsync(null, null, null)).ReturnsAsync(1);

        var filter = new PermissionFilterRequest(null, null, null, 1, 20);
        var result = await _sut.GetFilteredAsync(filter);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetMyAsync_ReturnsEmployeePermissions()
    {
        _tenant.Setup(t => t.CurrentEmployeeId).Returns(_employeeId);
        _repo.Setup(r => r.GetMyAsync(_employeeId))
            .ReturnsAsync(new List<PermissionRequest>());

        var result = await _sut.GetMyAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PermissionRequest?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveAsync_SendsNotification()
    {
        var leaveType = MakeLeaveType();
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new PermissionRequest
            {
                Id = id,
                EmployeeId = _employeeId,
                LeaveType = leaveType,
                Status = "pending",
            });

        await _sut.ApproveAsync(id, "Aprobado");

        _notification.Verify(n => n.NotifyTenantAsync(
            "tenant-123",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_SendsNotification()
    {
        var leaveType = MakeLeaveType();
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new PermissionRequest
            {
                Id = id,
                EmployeeId = _employeeId,
                LeaveType = leaveType,
                Status = "pending",
            });

        await _sut.RejectAsync(id, "No aprobado");

        _notification.Verify(n => n.NotifyTenantAsync(
            "tenant-123",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }
}
