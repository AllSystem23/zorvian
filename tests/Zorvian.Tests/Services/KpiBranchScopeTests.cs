using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase D — KPIs y Rankings con dimensión de sucursal:
/// - RecordKpiValueAsync resuelve la sucursal del registro: explícita VALIDADA
///   contra la compañía → sucursal del empleado (donde trabaja, no donde navega
///   quien lo registra) → sucursal del contexto.
/// - GetRankingsAsync/GetKpiRecordsAsync acotan por filtro explícito ?? contexto ?? todas.
/// - GenerateRankingAsync filtra records por alcance, REEMPLAZA la generación
///   previa del mismo alcance (antes cada POST duplicaba posiciones) y calcula
///   posiciones DENTRO de cada sucursal conservando BranchId por renglón.
/// </summary>
public sealed class KpiBranchScopeTests
{
    private static readonly Guid DefId = Guid.NewGuid();

    private readonly Mock<IKpiRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly KpiService _sut;

    public KpiBranchScopeTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new KpiService(
            _repo.Object,
            _mapper.Object,
            _tenant.Object,
            _branchValidator.Object,
            _employeeRepo.Object);
    }

    private static KpiRecord Record(Guid? employeeId, decimal compliance, Guid? branchId = null) => new()
    {
        KpiDefinitionId = DefId,
        EmployeeId = employeeId,
        BranchId = branchId,
        CompliancePercentage = compliance,
        PeriodKey = "2026-10",
    };

    private static KpiRecord RecordWithEmployee(Guid employeeId, Guid? employeeBranch, decimal compliance) => new()
    {
        KpiDefinitionId = DefId,
        EmployeeId = employeeId,
        Employee = new Employee { FirstName = "Ana", LastName = "Lopez", BranchId = employeeBranch },
        BranchId = employeeBranch,
        CompliancePercentage = compliance,
        PeriodKey = "2026-10",
    };

    // ── RecordKpiValueAsync: resolución de la sucursal del registro ──────────

    [Fact]
    public async Task RecordKpiValue_ExplicitBranch_IsValidatedAndKept()
    {
        var branch = Guid.NewGuid();
        var record = Record(null, 100, branch);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(branch)).ReturnsAsync(branch);

        var result = await _sut.RecordKpiValueAsync(record);

        result.BranchId.Should().Be(branch);
        _branchValidator.Verify(v => v.ResolveForWriteAsync(branch), Times.Once,
            "la sucursal explícita debe validarse contra la compañía");
        _employeeRepo.Verify(e => e.GetByIdAsync(It.IsAny<Guid>()), Times.Never,
            "con branch explícito no se consulta el empleado");
    }

    [Fact]
    public async Task RecordKpiValue_ExplicitForeignBranch_Throws()
    {
        var foreign = Guid.NewGuid();
        var record = Record(null, 100, foreign);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(foreign))
            .ThrowsAsync(new InvalidOperationException("La sucursal indicada no pertenece a la compañía."));

        var act = async () => await _sut.RecordKpiValueAsync(record);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no pertenece*");
    }

    [Fact]
    public async Task RecordKpiValue_EmployeeBranch_WinsOverContext()
    {
        var employeeBranch = Guid.NewGuid();
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        var employeeId = Guid.NewGuid();
        _employeeRepo.Setup(e => e.GetByIdAsync(employeeId))
            .ReturnsAsync(new Employee { FirstName = "Ana", LastName = "Lopez", BranchId = employeeBranch });

        var result = await _sut.RecordKpiValueAsync(Record(employeeId, 100));

        result.BranchId.Should().Be(employeeBranch,
            "la dimensión de reporte es la sucursal del empleado, no la del usuario que navega");
        _branchValidator.Verify(v => v.ResolveForWriteAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RecordKpiValue_NoExplicitNoEmployee_FallsBackToContext()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);

        var result = await _sut.RecordKpiValueAsync(Record(null, 100));

        result.BranchId.Should().Be(contextBranch);
    }

    [Fact]
    public async Task RecordKpiValue_EmptyGuidTreatedAsAbsent_UsesEmployeeBranch()
    {
        // Guid.Empty en el payload = sin sucursal explícita (convención del proyecto).
        var employeeBranch = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        _employeeRepo.Setup(e => e.GetByIdAsync(employeeId))
            .ReturnsAsync(new Employee { FirstName = "Ana", LastName = "Lopez", BranchId = employeeBranch });

        var result = await _sut.RecordKpiValueAsync(Record(employeeId, 100, Guid.Empty));

        result.BranchId.Should().Be(employeeBranch);
    }

    // ── Lecturas: scope explícito ?? contexto ?? todas ───────────────────────

    [Fact]
    public async Task GetRankings_UsesContextBranch_WhenNoExplicitFilter()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        _repo.Setup(r => r.GetRankingsByTypeAsync("sales", "2026-10", contextBranch))
            .ReturnsAsync(new List<Ranking>());

        await _sut.GetRankingsAsync("2026-10", "sales");

        _repo.Verify(r => r.GetRankingsByTypeAsync("sales", "2026-10", contextBranch), Times.Once);
    }

    [Fact]
    public async Task GetRankings_ExplicitBranch_WinsOverContext()
    {
        var contextBranch = Guid.NewGuid();
        var explicitBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        _repo.Setup(r => r.GetRankingsByPeriodAsync("2026-10", explicitBranch))
            .ReturnsAsync(new List<Ranking>());

        await _sut.GetRankingsAsync("2026-10", null, explicitBranch);

        _repo.Verify(r => r.GetRankingsByPeriodAsync("2026-10", explicitBranch), Times.Once);
    }

    [Fact]
    public async Task GetRecords_PeriodQuery_PassesScopeToRepo()
    {
        var contextBranch = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        _repo.Setup(r => r.GetKpiRecordsByPeriodAsync("2026-10", contextBranch))
            .ReturnsAsync(new List<KpiRecord>());

        var result = await _sut.GetKpiRecordsAsync(null, null, "2026-10");

        result.Should().BeEmpty();
        _repo.Verify(r => r.GetKpiRecordsByPeriodAsync("2026-10", contextBranch), Times.Once);
    }

    // ── Generación de rankings ───────────────────────────────────────────────

    [Fact]
    public async Task GenerateRanking_ScopeEmpty_RanksWithinEachBranch_AndStampsBranch()
    {
        // "Todas las sucursales": posiciones densas 1..n POR SUCURSAL; cada
        // renglón lleva la sucursal de su empleado.
        var branchA = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        var empC = Guid.NewGuid();
        _repo.Setup(r => r.GetKpiRecordsByPeriodAsync("2026-10", Guid.Empty))
            .ReturnsAsync(new List<KpiRecord>
            {
                RecordWithEmployee(empA, branchA, 90),
                RecordWithEmployee(empB, branchA, 80),
                RecordWithEmployee(empC, branchB, 95),
            });

        var result = await _sut.GenerateRankingAsync("2026-10", "compliance");

        var branchARows = result.Where(r => r.BranchId == branchA).OrderBy(r => r.Position).ToList();
        branchARows.Should().HaveCount(2);
        branchARows[0].EntityId.Should().Be(empA, "el 90% debe ser #1 de la sucursal A");
        branchARows[0].Position.Should().Be(1);
        branchARows[1].EntityId.Should().Be(empB);
        branchARows[1].Position.Should().Be(2);

        var branchBRows = result.Where(r => r.BranchId == branchB).ToList();
        branchBRows.Should().HaveCount(1);
        branchBRows[0].Position.Should().Be(1, "cada sucursal arranca en 1");
        branchBRows[0].EntityId.Should().Be(empC);

        // Regeneración idempotente: borra el alcance ANTES de insertar (antes
        // cada POST acumulaba filas y duplicaba posiciones en el período).
        _repo.Verify(r => r.DeleteRankingsAsync("2026-10", "compliance", Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task GenerateRanking_ContextBranch_ScopesAndStampsContextBranch()
    {
        var contextBranch = Guid.NewGuid();
        var empA = Guid.NewGuid();
        var empB = Guid.NewGuid();
        _tenant.Setup(t => t.CurrentBranchId).Returns(contextBranch);
        _repo.Setup(r => r.GetKpiRecordsByPeriodAsync("2026-10", contextBranch))
            .ReturnsAsync(new List<KpiRecord>
            {
                RecordWithEmployee(empA, contextBranch, 70),
                RecordWithEmployee(empB, contextBranch, 85),
            });

        var result = await _sut.GenerateRankingAsync("2026-10", "compliance");

        _repo.Verify(r => r.GetKpiRecordsByPeriodAsync("2026-10", contextBranch), Times.Once);
        _repo.Verify(r => r.DeleteRankingsAsync("2026-10", "compliance", contextBranch), Times.Once);
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.BranchId == contextBranch,
            "con alcance concreto todos los renglones llevan ese alcance");
        result.Select(r => r.Position).OrderBy(p => p).Should().Equal(1, 2);
    }

    [Fact]
    public async Task GenerateRanking_ExplicitRequestBranch_DeletesOnlyThatScope()
    {
        var branchA = Guid.NewGuid();
        var empA = Guid.NewGuid();
        _repo.Setup(r => r.GetKpiRecordsByPeriodAsync("2026-10", branchA))
            .ReturnsAsync(new List<KpiRecord> { RecordWithEmployee(empA, branchA, 50) });

        await _sut.GenerateRankingAsync("2026-10", "compliance", branchA);

        // El borrado va acotado al alcance: la otra sucursal NO se toca.
        _repo.Verify(r => r.DeleteRankingsAsync("2026-10", "compliance", branchA), Times.Once);
        _repo.Verify(r => r.DeleteRankingsAsync("2026-10", "compliance", Guid.Empty), Times.Never);
    }

    [Fact]
    public async Task GenerateRanking_FallsBackToRecordBranch_WhenEmployeeHasNone()
    {
        var recordBranch = Guid.NewGuid();
        var empX = Guid.NewGuid();
        var record = RecordWithEmployee(empX, null, 60);
        record.BranchId = recordBranch;
        _repo.Setup(r => r.GetKpiRecordsByPeriodAsync("2026-10", Guid.Empty))
            .ReturnsAsync(new List<KpiRecord> { record });

        var result = await _sut.GenerateRankingAsync("2026-10", "compliance");

        result.Should().HaveCount(1);
        result[0].BranchId.Should().Be(recordBranch,
            "sin sucursal del empleado se usa la sucursal de sus records");
    }
}
