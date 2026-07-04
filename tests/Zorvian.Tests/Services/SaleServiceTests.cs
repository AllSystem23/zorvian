using AutoMapper;
using Moq;
using Xunit;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<IClientRepository> _clientRepo = new();
    private readonly Mock<ICreditRepository> _creditRepo = new();
    private readonly Mock<IAutoAccountingService> _autoAccounting = new();
    private readonly Mock<IWebhookService> _webhook = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IGoalIntegrationService> _goalIntegration = new();
    private readonly Mock<IAccountingPeriodRepository> _periodRepo = new();
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid().ToString());
        var companyGuid = Guid.NewGuid();
        _periodRepo.Setup(r => r.GetCurrentOpenAsync(companyGuid)).ReturnsAsync(new AccountingPeriod { Id = companyGuid, Status = "open" });
        _periodRepo.Setup(r => r.GetCurrentOpenAsync(It.IsAny<Guid>())).ReturnsAsync(new AccountingPeriod { Id = Guid.NewGuid(), Status = "open" });
        _mapper.Setup(m => m.Map<Sale>(It.IsAny<CreateCashSaleRequest>()))
            .Returns<CreateCashSaleRequest>(req => new Sale
            {
                Id = Guid.NewGuid(),
                TenantId = _tenant.Object.TenantId,
                ClientId = req.ClientId,
                BranchId = req.BranchId,
                CurrencyCode = req.CurrencyCode ?? "NIO",
                SaleType = "cash",
                SaleDate = DateTime.UtcNow,
            });
        _mapper.Setup(m => m.Map<SaleResponse>(It.IsAny<Sale>()))
            .Returns<Sale>(s => new SaleResponse(
                s.Id, s.InvoiceNumber ?? "", s.ClientId, "", Guid.Empty, "",
                s.SaleDate, s.SaleType, s.Subtotal, s.Tax, s.Discount, s.Total,
                s.PaidAmount, s.Balance, s.Status ?? "", s.Notes,
                "NIO", null, new List<SaleDetailItem>(), null));
        _sut = new SaleService(
            _saleRepo.Object,
            _productRepo.Object,
            _movementRepo.Object,
            _companyRepo.Object,
            _clientRepo.Object,
            _creditRepo.Object,
            _autoAccounting.Object,
            _webhook.Object,
            _tenant.Object,
            _mapper.Object,
            _goalIntegration.Object,
            _periodRepo.Object);
    }

    [Fact]
    public async Task CreateCashSale_ShouldReduceStock()
    {
        var product = new Product { Id = Guid.NewGuid(), Stock = 10, SellingPrice = 100, CostPrice = 50 };
        _productRepo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _companyRepo.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>())).ReturnsAsync(new CompanySettings { TaxRate = 0.15m });

        _saleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Sale
            {
                Id = id,
                InvoiceNumber = "FAC-001",
                ClientId = Guid.NewGuid(),
                SaleDate = DateTime.UtcNow,
                SaleType = "cash",
                TenantId = _tenant.Object.TenantId,
                Details = new List<SaleDetail>(),
            });

        var request = new CreateCashSaleRequest(
            ClientId: Guid.NewGuid(), EmployeeId: Guid.NewGuid(), Discount: 0,
            Notes: null, BranchId: Guid.NewGuid(), CurrencyCode: "NIO", ExchangeRateToReporting: null,
            Details: [new SaleDetailItem(product.Id, "Test", 2, 100, 0, 200)],
            Payment: new SalePaymentInfo(200, "cash", null, null));

        await _sut.CreateCashSaleAsync(request);

        Assert.Equal(8, product.Stock);
    }

    [Fact]
    public async Task CreateCashSale_WithInsufficientStock_ShouldThrow()
    {
        var product = new Product { Id = Guid.NewGuid(), Stock = 1, Name = "Test Product" };
        _productRepo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _companyRepo.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>())).ReturnsAsync(new CompanySettings { TaxRate = 0.15m });

        var request = new CreateCashSaleRequest(
            ClientId: Guid.NewGuid(), EmployeeId: Guid.NewGuid(), Discount: 0,
            Notes: null, BranchId: Guid.NewGuid(), CurrencyCode: "NIO", ExchangeRateToReporting: null,
            Details: [new SaleDetailItem(product.Id, "Test", 5, 100, 0, 500)],
            Payment: new SalePaymentInfo(500, "cash", null, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateCashSaleAsync(request));
    }
}
