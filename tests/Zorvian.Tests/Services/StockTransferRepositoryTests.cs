using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal del StockTransferRepository. Una
/// transferencia NO tiene columna propia (abarca 2 sucursales): el filtro es
/// From == b OR To == b; Guid.Empty = todas. Los Include de las navs de
/// sucursal son INNER JOIN (FK requerida), así que se siembran ambas filas.
/// </summary>
public sealed class StockTransferRepositoryTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly StockTransferRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _branchC = Guid.NewGuid();

    public StockTransferRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new StockTransferRepository(_db);

        SeedBranch(_branchA, "Sucursal Central");
        SeedBranch(_branchB, "Sucursal Norte");
        SeedBranch(_branchC, "Sucursal Este");
        SeedTransfer(_branchA, _branchB, "TR-AB", "Refacciones bomba");
        SeedTransfer(_branchB, _branchC, "TR-BC", "Papel de oficina");
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

    private void SeedTransfer(Guid fromId, Guid toId, string number, string title)
    {
        var productId = Guid.NewGuid();
        _db.Products.Add(new Product
        {
            Id = productId,
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = fromId,
            Code = $"P{number}",
            Name = title,
            Stock = 10,
            CostPrice = 10m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });

        var transferId = Guid.NewGuid();
        _db.StockTransfers.Add(new StockTransfer
        {
            Id = transferId,
            TenantId = _tenantId,
            CompanyId = _company,
            FromBranchId = fromId,
            ToBranchId = toId,
            TransferNumber = number,
            Status = "draft",
            Notes = title,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.StockTransferDetails.Add(new StockTransferDetail
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            StockTransferId = transferId,
            ProductId = productId,
            Quantity = 5,
            UnitCost = 10m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetFiltered_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, Guid.Empty, 1, 20);

        Assert.Equal(2, total);
        Assert.Contains(items, t => t.TransferNumber == "TR-AB");
        Assert.Contains(items, t => t.TransferNumber == "TR-BC");
    }

    [Fact]
    public async Task GetFiltered_PorOrigenODestino_DevuelveTransferenciasQueTocanLaSucursal()
    {
        // Sucursal B es destino en TR-AB y origen en TR-BC → ambas visibles.
        var (itemsB, totalB) = await _sut.GetFilteredAsync(null, null, _branchB, 1, 20);
        Assert.Equal(2, totalB);

        // Sucursal A solo participa en TR-AB.
        var (itemsA, totalA) = await _sut.GetFilteredAsync(null, null, _branchA, 1, 20);
        Assert.Equal(1, totalA);
        Assert.Equal("TR-AB", itemsA.Single().TransferNumber);
    }

    [Fact]
    public async Task GetFiltered_BranchInexistente_DevuelveVacio()
    {
        var (_, total) = await _sut.GetFilteredAsync(null, null, Guid.NewGuid(), 1, 20);

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetFiltered_PorBusquedaYEstado_Filtra()
    {
        // El search mira número y nombres de sucursal origen/destino.
        var (bySearch, totalSearch) = await _sut.GetFilteredAsync("Central", null, Guid.Empty, 1, 20);
        Assert.Equal(1, totalSearch);
        Assert.Equal("TR-AB", bySearch.Single().TransferNumber);

        var (_, totalStatus) = await _sut.GetFilteredAsync(null, "completed", Guid.Empty, 1, 20);
        Assert.Equal(0, totalStatus); // no sembramos completadas

        var (_, totalDraft) = await _sut.GetFilteredAsync(null, "draft", Guid.Empty, 1, 20);
        Assert.Equal(2, totalDraft);
    }

    [Fact]
    public async Task GetById_IncluyeSucursalesDetalleYProducto()
    {
        var seeded = await _db.StockTransfers.IgnoreQueryFilters()
            .FirstAsync(t => t.TransferNumber == "TR-AB");

        var loaded = await _sut.GetByIdAsync(seeded.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Sucursal Central", loaded!.FromBranch?.Name);
        Assert.Equal("Sucursal Norte", loaded.ToBranch?.Name);
        var detail = loaded.Details.Single();
        Assert.Equal(5, detail.Quantity);
        Assert.Equal("Refacciones bomba", detail.Product?.Name);
    }

    [Fact]
    public async Task GenerateTransferNumber_EnInMemory_PrefijaTREIncrementa()
    {
        var first = await _sut.GenerateTransferNumberAsync(_company);

        // El servicio persiste la transferencia con ese número antes de generar
        // la siguiente (la query busca por prefijo en la tabla).
        _db.StockTransfers.Add(new StockTransfer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            FromBranchId = _branchA,
            ToBranchId = _branchB,
            TransferNumber = first,
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        await _db.SaveChangesAsync();

        var second = await _sut.GenerateTransferNumberAsync(_company);

        Assert.StartsWith($"TR-{DateTime.UtcNow:yyyyMMdd}-", first);
        Assert.Equal("0001", first[^4..]);
        Assert.Equal("0002", second[^4..]);
    }
}
