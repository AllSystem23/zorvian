using AutoMapper;
using MassTransit;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class PurchaseServiceTests
{
    private readonly Mock<IPurchaseRepository> _purchaseRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IApprovalEngine> _approvalEngine = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly PurchaseService _sut;

    public PurchaseServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        _sut = new PurchaseService(
            _purchaseRepo.Object,
            _productRepo.Object,
            _movementRepo.Object,
            _companyRepo.Object,
            _supplierRepo.Object,
            _autoAccounting.Object,
            _webhook.Object,
            _tenant.Object,
            _mapper.Object,
            _approvalEngine.Object,
            _publishEndpoint.Object);
    }

    [Fact]
    public async Task GetById_WhenFound_ReturnsResponse()
    {
        var id = Guid.NewGuid();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier" };
        var purchase = new Purchase
        {
            Id = id, SupplierId = supplier.Id, Supplier = supplier, Total = 1000,
            Status = "pending", PurchaseNumber = "P-001", CreatedAt = DateTime.UtcNow,
            PurchaseDate = DateTime.UtcNow,
            Details = [new PurchaseDetail { ProductId = Guid.NewGuid(), Quantity = 2, UnitCost = 500, Subtotal = 1000 }]
        };
        _purchaseRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(purchase);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("P-001", result!.PurchaseNumber);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNull()
    {
        _purchaseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Purchase?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
