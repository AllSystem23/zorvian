using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal del KpiRepository + borrado acotado de rankings.
/// Guid.Empty/null = todas las sucursales (guardia ApplyBranchScope); un GUID concreto
/// filtra por igualdad exacta. El Include(KpiDefinition) actúa como INNER JOIN en
/// in-memory → la definición debe estar sembrada o el repo devuelve 0 filas.
/// </summary>
public sealed class KpiRepositoryBranchScopeTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly KpiRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _defId = Guid.NewGuid();

    public KpiRepositoryBranchScopeTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new KpiRepository(_db);

        _db.KpiDefinitions.Add(new KpiDefinition
        {
            Id = _defId,
            TenantId = _tenantId,
            CompanyId = _company,
            Name = "Ventas cumplidas",
            KpiCategory = "sales",
            Formula = "%",
            DataSource = "sales",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();

        SeedRecord(_branchA, 90);
        SeedRecord(_branchB, 80);
        SeedRecord(null, 70); // record legado/sin sucursal

        SeedRanking(_branchA, "compliance", "2026-10", 1);
        SeedRanking(_branchB, "compliance", "2026-10", 1);
        SeedRanking(null, "compliance", "2026-10", 2);
        SeedRanking(_branchA, "sales", "2026-10", 1);      // otro tipo
        SeedRanking(_branchA, "compliance", "2026-09", 1); // otro período
    }

    public void Dispose() => _db.Dispose();

    private void SeedRecord(Guid? branchId, decimal compliance)
    {
        _db.KpiRecords.Add(new KpiRecord
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            KpiDefinitionId = _defId,
            BranchId = branchId,
            ActualValue = compliance,
            CompliancePercentage = compliance,
            EvaluationDate = new DateOnly(2026, 10, 15),
            PeriodKey = "2026-10",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    private void SeedRanking(Guid? branchId, string type, string period, int position)
    {
        _db.Rankings.Add(new Ranking
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            RankingType = type,
            PeriodKey = period,
            Position = position,
            EntityId = Guid.NewGuid(),
            EntityName = $"Ent {branchId}-{type}-{period}-{position}",
            Value = 90,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetRecordsByPeriod_EmptyBranch_ReturnsAllBranches_IncludingNull()
    {
        var rows = await _sut.GetKpiRecordsByPeriodAsync("2026-10", Guid.Empty);

        rows.Should().HaveCount(3, "Guid.Empty = todas las sucursales (incluye el record sin branch)");
    }

    [Fact]
    public async Task GetRecordsByPeriod_ConcreteBranch_FiltersExactly()
    {
        var rows = await _sut.GetKpiRecordsByPeriodAsync("2026-10", _branchA);

        rows.Should().ContainSingle();
        rows[0].BranchId.Should().Be(_branchA, "igualdad exacta: los sin sucursal NO entran en un filtro concreto");
    }

    [Fact]
    public async Task GetRecordsByPeriod_UnknownBranch_ReturnsEmpty()
    {
        var rows = await _sut.GetKpiRecordsByPeriodAsync("2026-10", Guid.NewGuid());

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRankingsByType_EmptyBranch_ReturnsAll_AndConcreteBranch_Filters()
    {
        var all = await _sut.GetRankingsByTypeAsync("compliance", "2026-10", Guid.Empty);
        all.Should().HaveCount(3, "compliance/2026-10 tiene renglones en A, B y sin sucursal");

        var onlyA = await _sut.GetRankingsByTypeAsync("compliance", "2026-10", _branchA);
        onlyA.Should().ContainSingle();
        onlyA[0].BranchId.Should().Be(_branchA);
    }

    [Fact]
    public async Task DeleteRankings_EmptyScope_RemovesAllOfPeriodAndType_KeepsOthers()
    {
        await _sut.DeleteRankingsAsync("2026-10", "compliance", Guid.Empty);
        await _db.SaveChangesAsync();

        var remain = await _db.Rankings.IgnoreQueryFilters().ToListAsync();
        remain.Should().HaveCount(2, "solo quedan el otro tipo (sales) y el otro período (2026-09)");
        remain.Should().OnlyContain(r =>
            (r.RankingType == "sales" && r.PeriodKey == "2026-10") ||
            (r.RankingType == "compliance" && r.PeriodKey == "2026-09"));
    }

    [Fact]
    public async Task DeleteRankings_ConcreteScope_RemovesOnlyThatBranch_KeepsOthers()
    {
        await _sut.DeleteRankingsAsync("2026-10", "compliance", _branchA);
        await _db.SaveChangesAsync();

        var remain = await _db.Rankings.IgnoreQueryFilters()
            .Where(r => r.PeriodKey == "2026-10" && r.RankingType == "compliance")
            .ToListAsync();

        remain.Should().HaveCount(2, "la sucursal B y el renglón sin sucursal no se tocan");
        remain.Should().NotContain(r => r.BranchId == _branchA);
        remain.Should().Contain(r => r.BranchId == _branchB);
        remain.Should().Contain(r => r.BranchId == null);
    }
}
