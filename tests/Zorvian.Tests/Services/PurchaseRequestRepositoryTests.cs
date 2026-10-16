using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal del PurchaseRequestRepository.
/// Guid.Empty = todas las sucursales (guardia ApplyBranchScope), un GUID concreto
/// filtra por igualdad; el Include(Branch) exige sembrar las filas de sucursal
/// porque la FK es requerida (INNER JOIN).
/// </summary>
public sealed class PurchaseRequestRepositoryTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly PurchaseRequestRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();

    public PurchaseRequestRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new PurchaseRequestRepository(_db);

        SeedBranch(_branchA, "Sucursal Central");
        SeedBranch(_branchB, "Sucursal Norte");
        SeedRequest(_branchA, "SC-A", "Solicitud laptops Central");
        SeedRequest(_branchB, "SC-B", "Solicitud refacciones Norte");
    }

    public void Dispose() => _db.Dispose();

    private void SeedBranch(Guid id, string name)
    {
        _db.Branches.Add(new Branch
        {
            Id = id,
            TenantId = _tenantId,
            CompanyId = _company,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    private void SeedRequest(Guid branchId, string number, string title)
    {
        var productId = Guid.NewGuid();
        _db.Products.Add(new Product
        {
            Id = productId,
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = branchId,
            Code = $"P{number}",
            Name = "Producto",
            Stock = 10,
            CostPrice = 10m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });

        var requestId = Guid.NewGuid();
        _db.PurchaseRequests.Add(new PurchaseRequest
        {
            Id = requestId,
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = branchId,
            RequestNumber = number,
            Title = title,
            Priority = "normal",
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.PurchaseRequestDetails.Add(new PurchaseRequestDetail
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = branchId,
            PurchaseRequestId = requestId,
            ProductId = productId,
            Quantity = 2,
            EstimatedUnitPrice = 10m,
            Subtotal = 20m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetFiltered_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, null, Guid.Empty, 1, 20);

        Assert.Equal(2, total);
        Assert.Contains(items, r => r.RequestNumber == "SC-A");
        Assert.Contains(items, r => r.RequestNumber == "SC-B");
    }

    [Fact]
    public async Task GetFiltered_ConBranchEspecifico_DevuelveSoloEsaSucursal()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, null, _branchA, 1, 20);

        Assert.Equal(1, total);
        Assert.Equal("SC-A", items.Single().RequestNumber);
        Assert.Equal("Sucursal Central", items.Single().Branch?.Name); // el listado trae el nombre
    }

    [Fact]
    public async Task GetFiltered_BranchInexistente_DevuelveVacio()
    {
        var (_, total) = await _sut.GetFilteredAsync(null, null, null, Guid.NewGuid(), 1, 20);

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetFiltered_PorBusquedaEstadoYPrioridad_Filtra()
    {
        var (bySearch, totalSearch) = await _sut.GetFilteredAsync("refacciones", null, null, Guid.Empty, 1, 20);
        Assert.Equal(1, totalSearch);
        Assert.Equal("SC-B", bySearch.Single().RequestNumber);

        var (_, totalStatus) = await _sut.GetFilteredAsync(null, "approved", null, Guid.Empty, 1, 20);
        Assert.Equal(0, totalStatus); // no sembramos aprobadas

        var (_, totalPriority) = await _sut.GetFilteredAsync(null, null, "high", Guid.Empty, 1, 20);
        Assert.Equal(0, totalPriority); // no sembramos prioridad alta
    }

    [Fact]
    public async Task GetById_IncluyeSucursalDetalleYProducto()
    {
        var seeded = await _db.PurchaseRequests.IgnoreQueryFilters().FirstAsync(r => r.RequestNumber == "SC-A");

        var loaded = await _sut.GetByIdAsync(seeded.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Sucursal Central", loaded!.Branch?.Name);
        var detail = loaded.Details.Single();
        Assert.Equal(2, detail.Quantity);
        Assert.Equal("Producto", detail.Product?.Name);
    }

    [Fact]
    public async Task GenerateRequestNumber_EnInMemory_PrefijaSCEIncrementa()
    {
        var first = await _sut.GenerateRequestNumberAsync(_company);

        // Como en el flujo real, el servicio persiste la solicitud con ese número
        // antes de generar la siguiente (la query busca por prefijo en la tabla).
        _db.PurchaseRequests.Add(new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = _branchA,
            RequestNumber = first,
            Title = "Generada",
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        await _db.SaveChangesAsync();

        var second = await _sut.GenerateRequestNumberAsync(_company);

        Assert.StartsWith($"SC-{DateTime.UtcNow:yyyyMMdd}-", first);
        Assert.Equal("0001", first[^4..]);
        Assert.Equal("0002", second[^4..]);
    }
}
