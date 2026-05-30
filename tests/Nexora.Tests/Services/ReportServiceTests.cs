using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Moq;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;
using Nexora.Infrastructure.Data;
using Nexora.Infrastructure.Services;

namespace Nexora.Tests.Services;

public sealed class ReportServiceTests : IDisposable
{
    private readonly NexoraDbContext _db;
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase($"ReportsTest_{Guid.NewGuid()}")
            .Options;
        _tenant.Setup(t => t.TenantId).Returns("tenant-123");
        _db = new NexoraDbContext(options, _tenant.Object);
        _sut = new ReportService(_db, _tenant.Object);

        SeedData();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private void SeedData()
    {
        var dept = new Department { Id = Guid.NewGuid(), TenantId = "tenant-123", Name = "IT", Code = "IT" };
        var emp = new Employee
        {
            Id = Guid.NewGuid(), TenantId = "tenant-123", FirstName = "Juan", LastName = "Pérez",
            EmployeeCode = "EMP-001", Email = "juan@test.com", HireDate = new DateOnly(2025, 1, 1),
            DepartmentId = dept.Id, Department = dept, Status = "active",
        };
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "VAC", Name = "Vacaciones" };

        _db.Departments.Add(dept);
        _db.Employees.Add(emp);
        _db.VacationRequests.Add(new VacationRequest
        {
            Id = Guid.NewGuid(), TenantId = "tenant-123", EmployeeId = emp.Id, Employee = emp,
            StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 5),
            TotalDays = 5, BusinessDays = 5, Status = "approved",
        });
        _db.PermissionRequests.Add(new PermissionRequest
        {
            Id = Guid.NewGuid(), TenantId = "tenant-123", EmployeeId = emp.Id, Employee = emp,
            LeaveTypeId = leaveType.Id, LeaveType = leaveType,
            StartDate = new DateOnly(2026, 6, 10), EndDate = new DateOnly(2026, 6, 10),
            TotalDays = 1, BusinessDays = 1, Status = "pending",
        });
        _db.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = Guid.NewGuid(), TenantId = "tenant-123", EmployeeId = emp.Id, Employee = emp,
            Date = new DateOnly(2026, 6, 1), CheckInTime = DateTime.UtcNow, Status = "present", TotalHours = 8,
        });
        _db.SaveChanges();
    }

    private void CheckExcelResult(byte[] data, string expectedSheetName)
    {
        Assert.NotNull(data);
        Assert.True(data.Length > 0);
        using var stream = new MemoryStream(data);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);
        Assert.NotNull(ws);
        Assert.Equal(expectedSheetName, ws.Name);
    }

    [Fact]
    public async Task GenerateVacationReportAsync_Should_Return_Valid_Excel()
    {
        var data = await _sut.GenerateVacationReportAsync(2026);
        CheckExcelResult(data, "Vacaciones");
    }

    [Fact]
    public async Task GeneratePermissionReportAsync_Should_Return_Valid_Excel()
    {
        var data = await _sut.GeneratePermissionReportAsync(2026);
        CheckExcelResult(data, "Permisos");
    }

    [Fact]
    public async Task GenerateAttendanceReportAsync_Should_Return_Valid_Excel()
    {
        var data = await _sut.GenerateAttendanceReportAsync(2026, 6);
        CheckExcelResult(data, "Asistencia");
    }

    [Fact]
    public async Task GenerateBalanceReportAsync_Should_Return_Valid_Excel()
    {
        var data = await _sut.GenerateBalanceReportAsync();
        CheckExcelResult(data, "Saldos");
    }
}
