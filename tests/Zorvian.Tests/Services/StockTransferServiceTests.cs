using FluentAssertions;
using Moq;
using Zorvian.Application.DTOs.StockTransfer;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Transferencia entre bodegas: creación con resolución de producto por sucursal
/// origen (el catálogo es por sucursal), despacho (baja + transfer_out),
/// recepción (alta promedio o clon + transfer_in) y guards de máquina de estados.
/// </summary>
public sealed class StockTransferServiceTests
{
    private readonly Mock<IStockTransferRepository> _repo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<IBranchValidator> _branchValidator = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly StockTransferService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();
    private readonly Guid _product = Guid.NewGuid();

    public StockTransferServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _repo.Setup(r => r.GenerateTransferNumberAsync(It.IsAny<Guid>()))
            .ReturnsAsync("TR-20261016-0001");
        _branchValidator.Setup(v => v.ResolveForWriteAsync(_branchA)).ReturnsAsync(_branchA);
        _branchValidator.Setup(v => v.ResolveForWriteAsync(_branchB)).ReturnsAsync(_branchB);

        _sut = new StockTransferService(
            _repo.Object, _productRepo.Object, _movementRepo.Object,
            _branchValidator.Object, _webhook.Object, _tenant.Object);
    }

    private CreateStockTransferRequest RequestB(Guid from, Guid? to, params StockTransferLineDto[] lines) =>
        new(from, to, null, lines.ToList());

    private static StockTransferLineDto Line(int qty = 5) => new(Guid.NewGuid(), qty);

    private Product OriginProduct(int stock = 10, decimal cost = 45m) => new()
    {
        Id = _product,
        Code = "PRD-1",
        Name = "Refacciones Bomba",
        BranchId = _branchA,
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
        FromBranchId = _branchA,
        FromBranch = new Branch { Id = _branchA, Name = "Central" },
        ToBranchId = _branchB,
        ToBranch = new Branch { Id = _branchB, Name = "Norte" },
        Status = status,
        CreatedBy = "carlos@zorvian.com",
    };

    [Fact]
    public async Task Create_ResolvesOriginProductByCode_WhenSentProductBelongsToAnotherBranch()
    {
        // El productId viene del catálogo offline y apunta a otra sucursal:
        // se remapea por código contra la sucursal ORIGEN.
        var originInBranchA = OriginProduct();
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Product { Id = Guid.NewGuid(), Code = "PRD-1", Name = "Refacciones Bomba", BranchId = _branchB, CostPrice = 99m });
        _productRepo.Setup(p => p.GetByCodeAsync("PRD-1", _branchA)).ReturnsAsync(originInBranchA);

        var result = await _sut.CreateAsync(RequestB(_branchA, _branchB, Line(5)));

        result.Status.Should().Be("draft");
        result.TransferNumber.Should().Be("TR-20261016-0001");
        result.FromBranchId.Should().Be(_branchA);
        result.ToBranchId.Should().Be(_branchB);

        _repo.Verify(r => r.AddAsync(It.Is<StockTransfer>(t =>
            t.Status == "draft" &&
            t.FromBranchId == _branchA &&
            t.ToBranchId == _branchB &&
            t.Details.Single().ProductId == originInBranchA.Id &&
            t.Details.Single().UnitCost == 45m)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_ProductMissingInOriginBranch_Throws()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Product { Id = Guid.NewGuid(), Code = "PRD-1", Name = "Otro", BranchId = _branchB });
        _productRepo.Setup(p => p.GetByCodeAsync("PRD-1", _branchA))
            .ReturnsAsync((Product?)null);

        var act = async () => await _sut.CreateAsync(RequestB(_branchA, _branchB, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*no existe en la sucursal origen*");
        _repo.Verify(r => r.AddAsync(It.IsAny<StockTransfer>()), Times.Never);
    }

    [Fact]
    public async Task Create_SameOriginAndDestination_Throws()
    {
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(OriginProduct());

        var act = async () => await _sut.CreateAsync(RequestB(_branchA, _branchA, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*distintas*");
    }

    [Fact]
    public async Task Create_MissingDestination_Throws()
    {
        // Sin destino explícito y sin sucursal en contexto → destino null.
        _branchValidator.Setup(v => v.ResolveForWriteAsync(null)).ReturnsAsync((Guid?)null);
        _productRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(OriginProduct());

        var act = async () => await _sut.CreateAsync(RequestB(_branchA, null, Line()));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*sucursal destino*");
    }

    [Fact]
    public async Task Create_WithoutLines_Throws()
    {
        var act = async () => await _sut.CreateAsync(RequestB(_branchA, _branchB));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*al menos un producto*");
    }

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
            BranchId = _branchB, Stock = 6, CostPrice = 40m,
        };
        _productRepo.Setup(p => p.GetByCodeAsync("PRD-1", _branchB)).ReturnsAsync(destination);

        var result = await _sut.ReceiveAsync(transfer.Id);

        result.Status.Should().Be("completed");
        transfer.ReceivedAt.Should().NotBeNull();
        destination.Stock.Should().Be(10, "6 existentes + 4 recibidas");
        destination.CostPrice.Should().Be(44m, "promedio ponderado: (40×6 + 50×4) / 10");

        _movementRepo.Verify(m => m.AddAsync(It.Is<InventoryMovement>(v =>
            v.MovementType == "transfer_in" &&
            v.ProductId == destination.Id &&
            v.Quantity == 4 &&
            v.StockBefore == 6 &&
            v.StockAfter == 10 &&
            v.UnitCost == 50m &&
            v.BranchId == _branchB)), Times.Once);
        _productRepo.Verify(p => p.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Receive_MissingDestination_ClonesProductCatalogRow()
    {
        var transfer = Stored("in_transit");
        transfer.Details = new List<StockTransferDetail>
        {
            new() { ProductId = _product, Quantity = 4, UnitCost = 50m },
        };
        _repo.Setup(r => r.GetByIdAsync(transfer.Id)).ReturnsAsync(transfer);
        _productRepo.Setup(p => p.GetByIdAsync(_product)).ReturnsAsync(OriginProduct(cost: 45m));
        _productRepo.Setup(p => p.GetByCodeAsync("PRD-1", _branchB))
            .ReturnsAsync((Product?)null);

        var result = await _sut.ReceiveAsync(transfer.Id);

        result.Status.Should().Be("completed");

        _productRepo.Verify(p => p.AddAsync(It.Is<Product>(c =>
            c.Code == "PRD-1" &&
            c.BranchId == _branchB &&
            c.Stock == 4 &&
            c.CostPrice == 50m &&
            c.TenantId == _companyId.ToString() &&
            c.CompanyId == _companyId)), Times.Once, "si la sucursal destino no maneja el producto se clona el catálogo");
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
    public async Task GetFiltered_WithoutBranch_PassesEmpty_AllBranches()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns((Guid?)null);
        _repo.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<StockTransfer>(), 0));

        await _sut.GetFilteredAsync(null, null, null, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, Guid.Empty, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetFiltered_ExplicitBranch_WinsOverContext()
    {
        _tenant.Setup(t => t.CurrentBranchId).Returns(Guid.NewGuid());
        _repo.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<Guid>(), 1, 20))
            .ReturnsAsync((new List<StockTransfer>(), 0));

        await _sut.GetFilteredAsync(null, null, _branchA, 1, 20);

        _repo.Verify(r => r.GetFilteredAsync(null, null, _branchA, 1, 20), Times.Once);
    }
}
