using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Consultas de saldo y ciclo de vida de vacaciones contra EF (in-memory).
/// Fuente de verdad = solicitudes (LeaveBalances es solo un espejo):
/// - taken solo cuenta "taken"; pending = "pending" + "approved" (las aprobadas RESERVAN saldo:
///   sin eso el saldo disponible quedaba sobreestimado y permitía re-programar días comprometidos).
/// - Medida BusinessDays-preferente (BusinessDays &gt; 0 ? BusinessDays : TotalDays).
/// - Año por StartDate; corrida de nómina por PaidByPayrollRunId.
/// </summary>
public sealed class VacationRepositoryTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly VacationRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _emp = Guid.NewGuid();
    private readonly Guid _otherEmp = Guid.NewGuid();

    public VacationRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new VacationRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private void Seed(Guid employeeId, string status, DateOnly start, DateOnly end,
        decimal businessDays, decimal totalDays, Guid? paidByRun = null)
    {
        _db.VacationRequests.Add(new VacationRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TenantId = _tenantId,
            StartDate = start,
            EndDate = end,
            BusinessDays = businessDays,
            TotalDays = totalDays,
            Status = status,
            PaidByPayrollRunId = paidByRun,
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetVacationBalanceSums_ReservesApprovedAndUsesBusinessDays()
    {
        Seed(_emp, "taken", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 14), 10, 14);
        Seed(_emp, "approved", new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 24), 8, 10);
        Seed(_emp, "pending", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 7), 5, 7);
        Seed(_emp, "rejected", new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 13), 7, 9);
        Seed(_emp, "taken", new DateOnly(2025, 12, 1), new DateOnly(2025, 12, 5), 3, 5);
        Seed(_otherEmp, "taken", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), 22, 30);

        var y2026 = await _sut.GetVacationBalanceSumsAsync(_emp, 2026);
        // taken usa BusinessDays (10), no TotalDays (14); aprobada (8) + pendiente (5) reservan.
        Assert.Equal(10m, y2026.TakenDays);
        Assert.Equal(13m, y2026.PendingDays);

        var career = await _sut.GetVacationBalanceSumsAsync(_emp, null);
        Assert.Equal(13m, career.TakenDays); // +3 de 2025
        Assert.Equal(13m, career.PendingDays);
    }

    [Fact]
    public async Task GetApprovedInRangeAsync_ReturnsOnlyOverlappingApproved()
    {
        Seed(_emp, "approved", new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 12), 3, 3); // dentro
        Seed(_emp, "approved", new DateOnly(2026, 5, 25), new DateOnly(2026, 6, 5), 9, 10);  // cruza el inicio
        Seed(_emp, "approved", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), 5, 5);    // fuera del rango
        Seed(_emp, "taken", new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21), 2, 2);     // ya tomada

        var result = await _sut.GetApprovedInRangeAsync(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal("approved", v.Status));
    }

    [Fact]
    public async Task GetByPayrollRunAsync_ReturnsOnlyRunLinkedVacations()
    {
        var runId = Guid.NewGuid();
        Seed(_emp, "taken", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), 5, 5, paidByRun: runId);
        Seed(_emp, "taken", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), 5, 5); // pagada por recibo RV
        Seed(_emp, "approved", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), 5, 5, paidByRun: runId);

        var result = await _sut.GetByPayrollRunAsync(runId);

        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal(runId, v.PaidByPayrollRunId));
    }
}
