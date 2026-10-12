using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

#pragma warning disable CS8602 // Selectores de sucursal transitiva: el helper guarda los navs nullable.

namespace Zorvian.Tests.Infrastructure;

/// <summary>
/// Contrato único de ApplyBranchScope: null/Empty = todas las sucursales;
/// cualquier otro GUID filtra por igualdad exacta. Es la semántica que deben
/// respetar todos los read-paths operativos.
/// </summary>
public sealed class BranchScopeExtensionsTests
{
    private sealed class Row
    {
        public Guid Id { get; init; }
        public Guid BranchId { get; set; }
    }

    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();

    private static List<Row> Seed() =>
    [
        new() { Id = Guid.NewGuid(), BranchId = BranchA },
        new() { Id = Guid.NewGuid(), BranchId = BranchB },
        new() { Id = Guid.NewGuid(), BranchId = BranchA },
    ];

    [Fact]
    public void ConBranchIdNull_DevuelveTodasLasSucursales()
    {
        var result = Seed().AsQueryable().ApplyBranchScope(null, r => r.BranchId).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ConBranchIdVacio_DevuelveTodasLasSucursales()
    {
        var result = Seed().AsQueryable().ApplyBranchScope(Guid.Empty, r => r.BranchId).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ConBranchIdConcreto_FiltraSoloEsaSucursal()
    {
        var result = Seed().AsQueryable().ApplyBranchScope(BranchA, r => r.BranchId).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(BranchA, r.BranchId));
    }

    [Fact]
    public void ConBranchIdInexistente_DevuelveVacio()
    {
        var result = Seed().AsQueryable().ApplyBranchScope(Guid.NewGuid(), r => r.BranchId).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void SeCombinaConOtrosFiltrosDelQuery()
    {
        var query = Seed().AsQueryable()
            .Where(r => r.BranchId == BranchA)
            .ApplyBranchScope(BranchA, r => r.BranchId);

        Assert.Equal(2, query.ToList().Count);
    }

    [Fact]
    public void ConBranchIdConcreto_GeneraSqlTraducible()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns("11111111-1111-1111-1111-111111111111");
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseNpgsql("Host=localhost;Database=zorvian_translation_check")
            .Options;
        using var db = new ZorvianDbContext(options, tenant.Object);

        var sql = db.Set<Quote>()
            .ApplyBranchScope(BranchA, q => q.BranchId)
            .ToQueryString();

        Assert.Contains("\"BranchId\" = @", sql);
    }

    [Fact]
    public void TransitivoConNavRequerida_GeneraSqlTraducible()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns("11111111-1111-1111-1111-111111111111");
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseNpgsql("Host=localhost;Database=zorvian_translation_check")
            .Options;
        using var db = new ZorvianDbContext(options, tenant.Object);

        var sql = db.Set<Trip>()
            .ApplyBranchScopeTransitive(BranchA, null, t => t.Vehicle.BranchId)
            .ToQueryString();

        Assert.Contains("\"BranchId\"", sql);
        Assert.Contains("@", sql);
    }

    [Fact]
    public void TransitivoConUnlinked_GeneraSqlTraducible()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns("11111111-1111-1111-1111-111111111111");
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseNpgsql("Host=localhost;Database=zorvian_translation_check")
            .Options;
        using var db = new ZorvianDbContext(options, tenant.Object);

        var sql = db.Set<Delivery>()
            .ApplyBranchScopeTransitive(
                BranchA,
                d => d.VehicleId == null && d.RouteId == null,
                d => d.Vehicle.BranchId,
                d => d.Route.BranchId)
            .ToQueryString();

        Assert.Contains("\"BranchId\"", sql);
        Assert.Contains("@", sql);
    }
}