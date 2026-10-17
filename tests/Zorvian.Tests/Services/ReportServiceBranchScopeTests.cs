using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase B: los exportes RRHH (vacaciones, permisos, asistencia, saldos) se acotan a la
/// sucursal activa del header y exponen la columna "Sucursal". Sin sucursal en el
/// contexto se exporta la compañía completa.
/// </summary>
public sealed class ReportServiceBranchScopeTests : IDisposable
{
    private const string TenantStr = "22222222-2222-2222-2222-222222222222";
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();

    private readonly string _dbName = "ReportSvc_" + Guid.NewGuid();
    private readonly Mock<ITenantContext> _tenant = new();

    public ReportServiceBranchScopeTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantStr);
        _tenant.Setup(t => t.SelectedCompanyId).Returns(CompanyId);
    }

    public void Dispose() { }

    private ZorvianDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new ZorvianDbContext(options, _tenant.Object);
    }

    private static Employee Emp(string name, Guid branchId, Guid tenantCompanyId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantStr,
        CompanyId = tenantCompanyId,
        FirstName = name,
        LastName = "Test",
        HireDate = new DateOnly(2024, 1, 1),
        Status = "active",
        BranchId = branchId,
    };

    private static VacationRequest Vacation(Guid employeeId, int month) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantStr,
        CompanyId = CompanyId,
        EmployeeId = employeeId,
        StartDate = new DateOnly(2026, month, 2),
        EndDate = new DateOnly(2026, month, 6),
        TotalDays = 5,
        BusinessDays = 5,
        Status = "pending",
    };

    private static Branch BranchRow(Guid id, string name) => new()
    {
        Id = id,
        TenantId = TenantStr,
        CompanyId = CompanyId,
        Name = name,
    };

    [Fact]
    public async Task GenerateVacationReportAsync_ConSucursal_FiltraEmpleadosYExponeColumna()
    {
        using var db = CreateDb();
        db.AddRange(BranchRow(BranchA, "Sucursal A"), BranchRow(BranchB, "Sucursal B"));
        var empA = Emp("Ana", BranchA, CompanyId);
        var empB = Emp("Beto", BranchB, CompanyId);
        db.AddRange(empA, empB);
        db.AddRange(Vacation(empA.Id, 3), Vacation(empB.Id, 3));
        await db.SaveChangesAsync();

        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);
        var service = new ReportService(db, _tenant.Object);

        var bytes = await service.GenerateVacationReportAsync(2026);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        var headers = HeaderRow(ws);
        Assert.Contains("Sucursal", headers);

        // Solo 1 fila de datos (Ana, de la sucursal activa).
        Assert.Equal(2, ws.LastRowUsed()!.RowNumber());
        Assert.Equal("Ana Test", ws.Cell(2, headers.IndexOf("Empleado") + 1).GetString());
        Assert.Equal("Sucursal A", ws.Cell(2, headers.IndexOf("Sucursal") + 1).GetString());
    }

    [Fact]
    public async Task GenerateVacationReportAsync_SinSucursal_ExportaTodaLaCompania()
    {
        using var db = CreateDb();
        var empA = Emp("Ana", BranchA, CompanyId);
        var empB = Emp("Beto", BranchB, CompanyId);
        db.AddRange(empA, empB);
        db.AddRange(Vacation(empA.Id, 3), Vacation(empB.Id, 4));
        await db.SaveChangesAsync();

        var service = new ReportService(db, _tenant.Object); // sin CurrentBranchId

        var bytes = await service.GenerateVacationReportAsync(2026);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        Assert.Equal(3, ws.LastRowUsed()!.RowNumber()); // encabezado + 2 empleados
    }

    [Fact]
    public async Task GenerateBalanceReportAsync_ConSucursal_SoloIncluyeEmpleadosDeEsaSucursal()
    {
        using var db = CreateDb();
        var empA = Emp("Ana", BranchA, CompanyId);
        var empB = Emp("Beto", BranchB, CompanyId);
        db.AddRange(empA, empB);
        await db.SaveChangesAsync();

        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);
        var service = new ReportService(db, _tenant.Object);

        var bytes = await service.GenerateBalanceReportAsync();

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        var headers = HeaderRow(ws);
        Assert.Contains("Sucursal", headers);
        Assert.Equal(2, ws.LastRowUsed()!.RowNumber()); // solo Ana
        Assert.Equal("Ana Test", ws.Cell(2, headers.IndexOf("Empleado") + 1).GetString());
    }

    [Fact]
    public async Task GenerateAttendanceReportAsync_ConSucursal_FiltraEmpleados()
    {
        using var db = CreateDb();
        var empA = Emp("Ana", BranchA, CompanyId);
        var empB = Emp("Beto", BranchB, CompanyId);
        db.AddRange(empA, empB);
        db.AddRange(
            new AttendanceRecord
            {
                Id = Guid.NewGuid(), TenantId = TenantStr, CompanyId = CompanyId,
                EmployeeId = empA.Id, Date = new DateOnly(2026, 5, 4), Status = "present",
            },
            new AttendanceRecord
            {
                Id = Guid.NewGuid(), TenantId = TenantStr, CompanyId = CompanyId,
                EmployeeId = empB.Id, Date = new DateOnly(2026, 5, 4), Status = "present",
            });
        await db.SaveChangesAsync();

        _tenant.Setup(t => t.CurrentBranchId).Returns(BranchA);
        var service = new ReportService(db, _tenant.Object);

        var bytes = await service.GenerateAttendanceReportAsync(2026, 5);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);
        var headers = HeaderRow(ws);
        Assert.Contains("Sucursal", headers);
        Assert.Equal(2, ws.LastRowUsed()!.RowNumber()); // solo Ana
    }

    private static List<string> HeaderRow(IXLWorksheet ws) =>
        Enumerable.Range(1, ws.LastColumnUsed()!.ColumnNumber())
            .Select(c => ws.Cell(1, c).GetString())
            .ToList();
}
