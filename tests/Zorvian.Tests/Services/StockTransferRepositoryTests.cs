using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad del StockTransferRepository por SUCURSAL y por BODEGA.
/// Una transferencia NO tiene columna BranchId propia (abarca 2 sucursales/2
/// bodegas): los filtros son "es origen O destino" (explícitos, no
/// ApplyBranchScope); Guid.Empty = todas. Los Include de las navs requeridas
/// son INNER JOIN en InMemory, así que se siembran sucursales, bodegas y
/// productos (fila por bodega).
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
    private readonly Guid _whA = Guid.NewGuid();
    private readonly Guid _whB = Guid.NewGuid();
    private readonly Guid _whC = Guid.NewGuid();

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
        SeedWarehouse(_whA, _branchA, "Bodega A");
        SeedWarehouse(_whB, _branchB, "Bodega B");
        SeedWarehouse(_whC, _branchC, "Bodega C");
        SeedTransfer(_whA, _whB, "TR-AB", "Refacciones bomba");
        SeedTransfer(_whB, _whC, "TR-BC", "Papel de oficina");
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

    private void SeedWarehouse(Guid id, Guid branchId, string name)
    {
        _db.Warehouses.Add(new Warehouse
        {
            Id = id,
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = branchId,
            Name = name,
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    private void SeedTransfer(Guid fromWh, Guid toWh, string number, string title)
    {
        var fromBranch = _db.Warehouses.First(w => w.Id == fromWh).BranchId;
        var toBranch = _db.Warehouses.First(w => w.Id == toWh).BranchId;

        var productId = Guid.NewGuid();
        _db.Products.Add(new Product
        {
            Id = productId,
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = fromBranch,
            WarehouseId = fromWh,
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
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            FromBranchId = fromBranch,
            ToBranchId = toBranch,
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
    public async Task GetFiltered_ConScopeVacio_DevuelveTodas()
    {
        var (items, total) = await _sut.GetFilteredAsync(null, null, Guid.Empty, Guid.Empty, 1, 20);

        Assert.Equal(2, total);
        Assert.Contains(items, t => t.TransferNumber == "TR-AB");
        Assert.Contains(items, t => t.TransferNumber == "TR-BC");
    }

    [Fact]
    public async Task GetFiltered_PorSucursalOrigenODestino_DevuelveLasQueTocanLaSucursal()
    {
        // Sucursal B es destino en TR-AB y origen en TR-BC → ambas visibles.
        var (itemsB, totalB) = await _sut.GetFilteredAsync(null, null, _branchB, Guid.Empty, 1, 20);
        Assert.Equal(2, totalB);

        // Sucursal A solo participa en TR-AB.
        var (itemsA, totalA) = await _sut.GetFilteredAsync(null, null, _branchA, Guid.Empty, 1, 20);
        Assert.Equal(1, totalA);
        Assert.Equal("TR-AB", itemsA.Single().TransferNumber);
    }

    [Fact]
    public async Task GetFiltered_PorBodegaOrigenODestino_DevuelveLasQueTocanLaBodega()
    {
        // Bodega B es destino en TR-AB y origen en TR-BC → ambas visibles.
        var (itemsB, totalB) = await _sut.GetFilteredAsync(null, null, Guid.Empty, _whB, 1, 20);
        Assert.Equal(2, totalB);

        // Bodega A solo participa en TR-AB.
        var (itemsA, totalA) = await _sut.GetFilteredAsync(null, null, Guid.Empty, _whA, 1, 20);
        Assert.Equal(1, totalA);
        Assert.Equal("TR-AB", itemsA.Single().TransferNumber);
    }

    [Fact]
    public async Task GetFiltered_BranchOWarehouseInexistente_DevuelveVacio()
    {
        var (_, byBranch) = await _sut.GetFilteredAsync(null, null, Guid.NewGuid(), Guid.Empty, 1, 20);
        Assert.Equal(0, byBranch);

        var (_, byWarehouse) = await _sut.GetFilteredAsync(null, null, Guid.Empty, Guid.NewGuid(), 1, 20);
        Assert.Equal(0, byWarehouse);
    }

    [Fact]
    public async Task GetFiltered_PorBusquedaYEstado_Filtra()
    {
        // El search mira número, sucursales y bodegas origen/destino.
        var (bySearch, totalSearch) = await _sut.GetFilteredAsync("Central", null, Guid.Empty, Guid.Empty, 1, 20);
        Assert.Equal(1, totalSearch);
        Assert.Equal("TR-AB", bySearch.Single().TransferNumber);

        var (byWarehouseSearch, totalWhSearch) = await _sut.GetFilteredAsync("Bodega B", null, Guid.Empty, Guid.Empty, 1, 20);
        Assert.Equal(2, totalWhSearch); // la bodega B participa en ambas transferencias

        var (_, totalStatus) = await _sut.GetFilteredAsync(null, "completed", Guid.Empty, Guid.Empty, 1, 20);
        Assert.Equal(0, totalStatus); // no sembramos completadas

        var (_, totalDraft) = await _sut.GetFilteredAsync(null, "draft", Guid.Empty, Guid.Empty, 1, 20);
        Assert.Equal(2, totalDraft);
    }

    [Fact]
    public async Task GetById_IncluyeSucursalesBodegasDetalleYProducto()
    {
        var seeded = await _db.StockTransfers.IgnoreQueryFilters()
            .FirstAsync(t => t.TransferNumber == "TR-AB");

        var loaded = await _sut.GetByIdAsync(seeded.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Sucursal Central", loaded!.FromBranch?.Name);
        Assert.Equal("Sucursal Norte", loaded.ToBranch?.Name);
        Assert.Equal("Bodega A", loaded.FromWarehouse?.Name);
        Assert.Equal("Bodega B", loaded.ToWarehouse?.Name);
        var detail = loaded.Details.Single();
        Assert.Equal(5, detail.Quantity);
        Assert.Equal(0, detail.QuantityReceived);
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
            FromWarehouseId = _whA,
            ToWarehouseId = _whB,
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
