using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.StockTransfer;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Transferencia entre bodegas: resolución de ORIGEN/DESTINO (bodega explícita,
/// sucursal→bodega por defecto, contexto), remapeo de producto por código contra
/// la bodega origen, despacho (baja + transfer_out), recepción total/parcial
/// (alta promedio o clon + transfer_in) y guards de máquina de estados.
/// </summary>
public sealed class StockTransferServiceTests
{
    private readonly Mock<IStockTransferRepository> _repo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepo = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly StockTransferService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _whA = Guid.NewGuid();
    private readonly Guid _whA2 = Guid.NewGuid();
    private readonly Guid _whB = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();

    public StockTransferServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _repo.Setup(r => r.GenerateTransferNumberAsync(It.IsAny<Guid>()))
            .ReturnsAsync("TR-20261016-0001");
        _branchValidator.Setup(v => v.ResolveForWriteAsync(_branchA)).ReturnsAsync(_branchA);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(_branchB)).ReturnsAsync(_branchB);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(null)).ReturnsAsync((Guid?)null);

        // Bodegas: A y A2 en la sucursal A; B en la sucursal B (A = default de A).
        _warehouseRepo.Setup(r => r.GetDefaultAsync(_branchA)).ReturnsAsync(Warehouse(_whA, _branchA, "Bodega A"));
        _warehouseRepo.Setup(r => r.GetDefaultAsync(_branchB)).ReturnsAsync(Warehouse(_whB, _branchB, "Bodega B"));
        _warehouseRepo.Setup(r => r.GetByIdAsync(_whA)).ReturnsAsync(Warehouse(_whA, _branchA, "Bodega A"));
        _warehouseRepo.Setup(r => r.GetByIdAsync(_whA2)).ReturnsAsync(Warehouse(_whA2, _branchA, "Bodega A2"));
        _warehouseRepo.Setup(r => r.GetByIdAsync(_whB)).ReturnsAsync(Warehouse(_whB, _branchB, "Bodega B"));
        _warehouseRepo.Setup(r => r.GetByBranchAsync(_branchA))
            .ReturnsAsync(new List<Warehouse> { Warehouse(_whA, _branchA, "Bodega A") });

        _sut = new StockTransferService(
            _repo.Object, _productRepo.Object, _movementRepo.Object,
            _warehouseRepo.Object, _branchValidator.Object, _webhook.Object, _tenant.Object);
    }

    private static Warehouse Warehouse(Guid id, Guid branchId, string name) => new()
    {
        Id = id,
        BranchId = branchId,
        Name = name,
        IsActive = true,
        IsDefault = true,
    };

    /// <summary>Solo sucursales (legacy): el servicio resuelve la bodega default.</summary>
    private CreateStockTransferRequest RequestB(Guid from, Guid? to, params StockTransferLineDto[] lines) =>
        new(null, null, from, to, null, lines.ToList());

    /// <summary>Bodegas explícitas (contrato nuevo).</summary>
    private CreateStockTransferRequest RequestW(Guid? fromWh, Guid? toWh, params StockTransferLineDto[] lines) =>
        new(fromWh, toWh, null, null, null, lines.ToList());

    private StockTransferLineDto Line(int qty = 5) => new(_product, qty);

    private Product OriginProduct(int stock = 10, decimal cost = 45m) => new()
    {
        Id = _product,
        Code = "PRD-1",
        Name = "Refacciones Bomba",
        BranchId = _branchA,
        WarehouseId = _whA,
        Stock = stock,
        CostPrice = cost,
        TenantId = _companyId.ToString(),
        CompanyId = _companyId,
        CreatedBy = "seed",
    };

    private StockTransfer Stored(string status) => new()
    {
        Id = Guid.NewGuid(),
        TransferNumber = "TR-20261016-0009",
        FromWarehouseId = _whA,
        FromWarehouse = Warehouse(_whA, _branchA, "Bodega A"),
        ToWarehouseId = _whB,
        ToWarehouse = Warehouse(_whB, _branchB, "Bodega B"),
        FromBranchId = _branchA,
        FromBranch = new Branch { Id = _branchA, Name = "Central" },
        ToBranchId = _branchB,
        ToBranch = new Branch { Id = _branchB, Name = "Norte" },
        Status = status,
        CreatedBy = "carlos@zorvian.com",
    };

    // ── Creación / resolución de bodegas ────────────────────────────────

    [Fact]
    public async Task Create_ExplicitWarehouses_DerivesBranchesFromThem()
    {
        var originInWhA = OriginProduct();
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(originInWhA);

        var result = await _sut.CreateAsync(RequestW(_whA, _whB, Line(5)));

        result.Status.Should().Be("draft");
        result.TransferNumber.Should().Be("TR-20261016-0001");
        result.FromWarehouseId.Should().Be(_whA);
        result.ToWarehouseId.Should().Be(_whB);
        result.FromBranchId.Should().Be(_branchA, "la sucursal origen se deriva de la bodega origen");
        result.ToBranchId.Should().Be(_branchB, "la sucursal destino se deriva de la bodega destino");

        _repo.Verify(r => r.AddAsync(It.Is<StockTransfer>(t =>
            t.Status == "draft" &&
            t.FromWarehouseId == _whA &&
            t.ToWarehouseId == _whB &&
            t.Details.Single().ProductId == _product &&
            t.Details.Single().UnitCost == 45m)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_LegacyBranches_ResolveDefaultWarehouses()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(OriginProduct());

        var result = await _sut.CreateAsync(RequestB(_branchA, _branchB, Line(5)));

        result.FromWarehouseId.Should().Be(_whA, "sin bodega explícita se usa la default de la sucursal");
        result.ToWarehouseId.Should().Be(_whB);
        result.FromBranchId.Should().Be(_branchA);
        result.ToBranchId.Should().Be(_branchB);
    }

    [Fact]
    public async Task Create_IntraBranchTransfer_DistinctWarehouses_IsAllowed()
    {
        // Misma sucursal, dos bodegas: transferencia interna — VÁLIDA hoy.
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct());

        var result = await _sut.CreateAsync(RequestW(_whA, _whA2, Line(5)));

        result.Status.Should().Be("draft");
        result.FromBranchId.Should().Be(_branchA);
        result.ToBranchId.Should().Be(_branchA);
        result.FromWarehouseId.Should().NotBe(result.ToWarehouseId);
    }

    [Fact]
    public async Task Create_SameWarehouseOriginAndDestination_Throws()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(OriginProduct());

        var act = async () => await _sut.CreateAsync(RequestW(_whA, _whA, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*distintas*");
    }

    [Fact]
    public async Task Create_UnknownWarehouse_Throws()
    {
        var missing = Guid.NewGuid();
        var act = async () => await _sut.CreateAsync(RequestW(missing, _whB, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no existe*");
        _repo.Verify(r => r.AddAsync(It.IsAny<StockTransfer>()), Times.Never);
    }

    [Fact]
    public async Task Create_ResolvesOriginProductByCode_WhenSentProductBelongsToAnotherWarehouse()
    {
        // El productId viene del catálogo offline (otra bodega): se remapea por
        // código contra la bodega ORIGEN.
        var originInWhA = OriginProduct();
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Product { Id = Guid.NewGuid(), Code = "PRD-1", Name = "Refacciones Bomba", BranchId = _branchB, WarehouseId = _whB, CostPrice = 99m });
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whA)).ReturnsAsync(originInWhA);

        var result = await _sut.CreateAsync(RequestW(_whA, _whB, Line(5)));

        _repo.Verify(r => r.AddAsync(It.Is<StockTransfer>(t =>
            t.Details.Single().ProductId == originInWhA.Id &&
            t.Details.Single().UnitCost == 45m)), Times.Once);
    }

    [Fact]
    public async Task Create_ProductMissingInOriginWarehouse_Throws()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Product { Id = Guid.NewGuid(), Code = "PRD-1", Name = "Otro", WarehouseId = _whB });
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whA))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.CreateAsync(RequestW(_whA, _whB, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no existe en la bodega origen*");
        _repo.Verify(r => r.AddAsync(It.IsAny<StockTransfer>()), Times.Never);
    }

    [Fact]
    public async Task Create_MissingDestination_Throws()
    {
        // Sin destino explícito, sin sucursal en contexto y sin bodega activa.
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(OriginProduct());

        var act = async () => await _sut.CreateAsync(RequestB(_branchA, null, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*bodega destino*");
    }

    [Fact]
    public async Task Create_WithoutLines_Throws()
    {
        var act = async () => await _sut.CreateAsync(RequestW(_whA, _whB));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*al menos un producto*");
    }

    // ── Despacho ────────────────────────────────────────────────────────

    [Fact]
    public async Task Dispatch_DecrementsOriginStock_AndRegistersTransferOut()
    {
        var transfer = Stored("draft");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { ProductId = _product, Quantity = 3 },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var product = OriginProduct(stock: 10, cost: 45m);
        _productRepo.Setup(p => p.GetByIdForUpdateAsync(_product)).ReturnsAsync(product);

        var result = await _sut.DispatchAsync(transfer.Id);

        result.Status.Should().Be("in_transit");
        product.Stock.Should().Be(7, "el despacho descuenta 3 unidades en el origen");
        transfer.DispatchedAt.Should().NotBeNull();
        transfer.Details.Single().UnitCost.Should().Be(45m, "el costo se congela al despachar");

        _movementRepo.Verify(m => m.AddAsync(It.Is<InventoryMovement>(v =>
            v.MovementType == "transfer_out" &&
            v.ProductId == _product &&
            v.Quantity == 3 &&
            v.StockBefore == 10 &&
            v.StockAfter == 7 &&
            v.UnitCost == 45m &&
            v.BranchId == _branchA &&
            v.ReferenceNumber == "TR-20261016-0009")), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "stock_transfer.dispatched");
    }

    [Fact]
    public async Task Dispatch_InsufficientStock_Throws_WithoutSaving()
    {
        var transfer = Stored("draft");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { ProductId = _product, Quantity = 3 },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdForUpdateAsync(_product))
            .ReturnsAsync(OriginProduct(stock: 2));

        var act = async () => await _sut.DispatchAsync(transfer.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*Stock insuficiente*");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _movementRepo.Verify(m => m.AddAsync(It.IsAny<InventoryMovement>()), Times.Never);
        transfer.Status.Should().Be("draft", "el error no debe mutar el estado");
    }

    [Fact]
    public async Task Dispatch_FromNotDraft_Throws()
    {
        var transfer = Stored("in_transit");
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var act = async () => await _sut.DispatchAsync(transfer.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*borrador*");
    }

    // ── Recepción total ─────────────────────────────────────────────────

    [Fact]
    public async Task Receive_ExistingDestination_AveragesCostAndRegistersTransferIn()
    {
        var transfer = Stored("in_transit");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { ProductId = _product, Quantity = 4, UnitCost = 50m },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct(cost: 45m));

        var destination = new Product
        {
            Id = Guid.NewGuid(), Code = "PRD-1", Name = "Refacciones Bomba",
            BranchId = _branchB, WarehouseId = _whB, Stock = 6, CostPrice = 40m,
        };
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whB)).ReturnsAsync(destination);

        var result = await _sut.ReceiveAsync(transfer.Id);

        result.Status.Should().Be("completed");
        transfer.ReceivedAt.Should().NotBeNull();
        destination.Stock.Should().Be(10, "6 existentes + 4 recibidas");
        destination.CostPrice.Should().Be(44m, "promedio ponderado: (40×6 + 50×4) / 10");
        transfer.Details.Single().QuantityReceived.Should().Be(4);

        _movementRepo.Verify(m => m.AddAsync(It.Is<InventoryMovement>(v =>
            v.MovementType == "transfer_in" &&
            v.ProductId == destination.Id &&
            v.Quantity == 4 &&
            v.StockBefore == 6 &&
            v.StockAfter == 10 &&
            v.UnitCost == 50m &&
            v.BranchId == _branchB)), Times.Once);
        _productRepo.Verify(p => p.AddAsync(It.IsAny<Product>()), Times.Never);
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "stock_transfer.completed");
    }

    [Fact]
    public async Task Receive_MissingDestination_ClonesProductIntoDestinationWarehouse()
    {
        var transfer = Stored("in_transit");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { ProductId = _product, Quantity = 4, UnitCost = 50m },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct(cost: 45m));
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whB))
            .ReturnsAsync((Product?)null);

        var result = await _sut.ReceiveAsync(transfer.Id);

        result.Status.Should().Be("completed");

        _productRepo.Verify(p => p.AddAsync(It.Is<Product>(c =>
            c.Code == "PRD-1" &&
            c.WarehouseId == _whB &&
            c.BranchId == _branchB &&
            c.Stock == 4 &&
            c.CostPrice == 50m &&
            c.TenantId == _companyId.ToString() &&
            c.CompanyId == _companyId)), Times.Once, "si la bodega destino no maneja el producto se clona la fila");
        _movementRepo.Verify(m => m.AddAsync(It.Is<InventoryMovement>(v =>
            v.MovementType == "transfer_in" &&
            v.StockBefore == 0 &&
            v.StockAfter == 4 &&
            v.UnitCost == 50m &&
            v.BranchId == _branchB)), Times.Once);
    }

    [Fact]
    public async Task Receive_FromNotInTransit_Throws()
    {
        var transfer = Stored("draft");
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var act = async () => await _sut.ReceiveAsync(transfer.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*tránsito*");
    }

    // ── Recepción parcial ───────────────────────────────────────────────

    [Fact]
    public async Task Receive_PartialLine_SetsPartiallyReceived_AndKeepsPending()
    {
        var transfer = Stored("in_transit");
        var detail = new StockTransferDetail
        {
            Id = Guid.NewGuid(),
            ProductId = _product,
            Quantity = 4,
            UnitCost = 50m,
        };
        transfer.Details = new List<StockTransferDetail> { detail };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct(cost: 45m));
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whB))
            .ReturnsAsync((Product?)null);

        var result = await _sut.ReceiveAsync(transfer.Id,
            new ReceiveStockTransferRequest(new List<ReceiveStockTransferLine>
            {
                new(detail.Id, 2),
            }));

        result.Status.Should().Be("partially_received");
        detail.QuantityReceived.Should().Be(2, "quedan 2 unidades pendientes");
        transfer.ReceivedAt.Should().NotBeNull();
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "stock_transfer.partially_received");
        _movementRepo.Verify(m => m.AddAsync(It.Is<InventoryMovement>(v =>
            v.Quantity == 2 && v.StockAfter == 2)), Times.Once);
    }

    [Fact]
    public async Task Receive_SecondBatchOfPending_CompletesTransfer()
    {
        var transfer = Stored("partially_received");
        var detail = new StockTransferDetail
        {
            Id = Guid.NewGuid(),
            ProductId = _product,
            Quantity = 4,
            QuantityReceived = 2,
            UnitCost = 50m,
        };
        transfer.Details = new List<StockTransferDetail> { detail };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct(cost: 45m));
        var destination = new Product
        {
            Id = Guid.NewGuid(), Code = "PRD-1", Name = "Refacciones Bomba",
            WarehouseId = _whB, Stock = 2, CostPrice = 50m,
        };
        _productRepo.Setup(p => p.GetByCodeForWarehouseAsync("PRD-1", _whB)).ReturnsAsync(destination);

        // Sin líneas = recibe TODO lo pendiente (2).
        var result = await _sut.ReceiveAsync(transfer.Id);

        result.Status.Should().Be("completed");
        detail.QuantityReceived.Should().Be(4);
        destination.Stock.Should().Be(4);
    }

    [Fact]
    public async Task Receive_QuantityExceedsPending_Throws()
    {
        var transfer = Stored("in_transit");
        var detail = new StockTransferDetail
        {
            Id = Guid.NewGuid(),
            ProductId = _product,
            Quantity = 4,
            UnitCost = 50m,
        };
        transfer.Details = new List<StockTransferDetail> { detail };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var act = async () => await _sut.ReceiveAsync(transfer.Id,
            new ReceiveStockTransferRequest(new List<ReceiveStockTransferLine>
            {
                new(detail.Id, 9),
            }));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*excede el pendiente*");
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Receive_EmptyLines_Throws()
    {
        var transfer = Stored("in_transit");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { Id = Guid.NewGuid(), ProductId = _product, Quantity = 4 },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var act = async () => await _sut.ReceiveAsync(transfer.Id,
            new ReceiveStockTransferRequest(new List<ReceiveStockTransferLine>()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*al menos un producto*");
    }

    // ── Cancelación / listado ───────────────────────────────────────────

    [Fact]
    public async Task Cancel_FromDraft_CancelsAndPublishes()
    {
        var transfer = Stored("draft");
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var result = await _sut.CancelAsync(transfer.Id);

        result.Status.Should().Be("cancelled");
        _webhook.Invocations.Should().ContainSingle(i =>
            i.Method.Name == nameof(IWebhookService.PublishAsync) &&
            (string)i.Arguments[1] == "stock_transfer.cancelled");
    }

    [Fact]
    public async Task Cancel_FromInTransit_Throws()
    {
        var transfer = Stored("in_transit");
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);

        var act = async () => await _sut.CancelAsync(transfer.Id);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*borrador*");
    }

    [Fact]
    public async Task GetFiltered_WithoutScope_PassesEmptyWarehouseAndBranch()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _tenant.Setup(t => t.CurrentWarehouseId).Returns((Guid?)null);
        _repo.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<Guid>(), It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<StockTransfer>(), 0));

        await _sut.GetFilteredAsync(null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, Guid.Empty, Guid.Empty, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetFiltered_ContextWarehouse_PassesItThrough()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _tenant.Setup(t => t.CurrentWarehouseId).Returns(_whA);
        _repo.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<Guid>(), It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<StockTransfer>(), 0));

        await _sut.GetFilteredAsync(null, null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, Guid.Empty, _whA, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetFiltered_ExplicitFilters_WinOverContext()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(Guid.NewGuid());
        _tenant.Setup(t => t.CurrentWarehouseId).Returns(Guid.NewGuid());
        _repo.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<Guid>(), It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<StockTransfer>(), 0));

        await _sut.GetFilteredAsync(null, null, _branchA, _whB, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, _branchA, _whB, 1, 20), Times.Once);
    }
}
