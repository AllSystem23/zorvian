using AutoMapper;
using Moq;
using FluentAssertions;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Tests.Services;

public sealed class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepo = new();
    private readonly Mock<ICompanyRepository> _companyRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly SaleService _sut;
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public SaleServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());

        var mockEntryRepo = new Mock<IAccountingEntryRepository>();
        var mockPeriodRepo = new Mock<IAccountingPeriodRepository>();
        var mockLinkRepo = new Mock<IAccountLinkRepository>(); // Setup properly
        mockLinkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new AccountLink { AccountId = Guid.NewGuid() });
            
        var mockRuleRepo = new Mock<IAccountingRuleRepository>();
        var mockAccountRepo = new Mock<IAccountRepository>();
        var mockPayrollRepo = new Mock<IPayrollRepository>();
        
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(t => t.TenantId).Returns(_companyId.ToString());
        mockAccountRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new Account { Id = Guid.NewGuid() });
            
        var mockCashRepo = new Mock<ICashMovementRepository>();
        
        var autoAccounting = new AutoAccountingService(
            mockEntryRepo.Object, mockPeriodRepo.Object, mockLinkRepo.Object, mockRuleRepo.Object, 
            mockAccountRepo.Object, mockTenantContext.Object, mockPayrollRepo.Object, mockCashRepo.Object);

        _sut = new SaleService(
            _saleRepo.Object,
            _productRepo.Object,
            _movementRepo.Object,
            _companyRepo.Object,
            autoAccounting,
            _tenant.Object,
            _mapper.Object);
    }
    // ... (rest of the file remains same)
    private Company MakeCompany() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = _companyId.ToString(),
    };

    private CompanySettings MakeSettings(bool taxEnabled = true, decimal taxRate = 0.16m) => new()
    {
        Id = Guid.NewGuid(),
        TaxEnabled = taxEnabled,
        TaxRate = taxRate,
        CompanyId = Guid.NewGuid(),
    };

    private Sale MakeSale(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        InvoiceNumber = "INV-001",
        ClientId = _clientId,
        SaleDate = DateTime.UtcNow,
        SaleType = "cash",
        Subtotal = 1000m,
        Tax = 160m,
        Discount = 0,
        Total = 1160m,
        PaidAmount = 1160m,
        Balance = 0,
        Status = "completed",
        CompanyId = _companyId,
        BranchId = _branchId,
    };

    [Fact]
    public async Task CreateCashSaleAsync_WithTaxEnabled_CalculatesTotalCorrectly()
    {
        var company = MakeCompany();
        var saleId = Guid.NewGuid();

        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync(company);

        var request = new CreateCashSaleRequest(
            _clientId, _employeeId, 0, null, _branchId,
            new List<SaleDetailItem>
            {
                new(Guid.NewGuid(), "Product A", 2, 500m, 0, 1000m),
            },
            new SalePaymentInfo(1160m, "cash", null, null));

        var product = new Product { 
            Id = request.Details[0].ProductId, 
            Stock = 10, 
            CostPrice = 300m,
            TaxCategory = new TaxCategory { Rate = 0.16m, Name = "16%" }
        };
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var sale = MakeSale(saleId);
        sale.Details = new List<SaleDetail> { new() { Product = product, Quantity = 2, UnitPrice = 500m, Discount = 0, Subtotal = 1000m } };
        _mapper.Setup(m => m.Map<Sale>(request)).Returns(sale);
        _saleRepo.Setup(r => r.GenerateInvoiceNumberAsync(company.Id)).ReturnsAsync("INV-001");

        var expectedResponse = new SaleResponse(
            saleId, "INV-001", _clientId, "Client", _employeeId, "Employee",
            sale.SaleDate, "cash", 1000m, 160m, 0, 1160m, 1160m, 0, "completed", null,
            new List<SaleDetailItem>(), null);

        _saleRepo.Setup(r => r.GetByIdAsync(saleId)).ReturnsAsync(sale);
        _mapper.Setup(m => m.Map<SaleResponse>(sale)).Returns(expectedResponse);

        var result = await _sut.CreateCashSaleAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(1160m);
        result.Tax.Should().Be(160m);
        result.Subtotal.Should().Be(1000m);
        _saleRepo.Verify(r => r.AddAsync(It.IsAny<Sale>()), Times.Once);
        _saleRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCashSaleAsync_WithTaxDisabled_SetsTotalEqualToSubtotal()
    {
        var company = MakeCompany();
        var settings = MakeSettings(taxEnabled: false);
        var saleId = Guid.NewGuid();

        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync(company);
        _companyRepo.Setup(r => r.GetSettingsAsync(company.Id)).ReturnsAsync(settings);

        var request = new CreateCashSaleRequest(
            _clientId, _employeeId, 0, null, _branchId,
            new List<SaleDetailItem>
            {
                new(Guid.NewGuid(), "Product A", 2, 500m, 0, 1000m),
            },
            new SalePaymentInfo(1000m, "cash", null, null));

        var sale = MakeSale(saleId);
        _mapper.Setup(m => m.Map<Sale>(request)).Returns(sale);
        _saleRepo.Setup(r => r.GenerateInvoiceNumberAsync(company.Id)).ReturnsAsync("INV-001");

        var expectedResponse = new SaleResponse(
            saleId, "INV-001", _clientId, "Client", _employeeId, "Employee",
            sale.SaleDate, "cash", 1000m, 0, 0, 1000m, 1000m, 0, "completed", null,
            new List<SaleDetailItem>(), null);

        _saleRepo.Setup(r => r.GetByIdAsync(saleId)).ReturnsAsync(sale);
        _mapper.Setup(m => m.Map<SaleResponse>(sale)).Returns(expectedResponse);

        var product = new Product { 
            Id = request.Details[0].ProductId, 
            Stock = 10, 
            CostPrice = 300m,
            TaxCategory = new TaxCategory { Rate = 0.0m, Name = "Exento" }
        };
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var result = await _sut.CreateCashSaleAsync(request);

        result.Should().NotBeNull();
        result.Total.Should().Be(1000m);
        result.Tax.Should().Be(0);
        result.Subtotal.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateCreditSaleAsync_CreatesCreditWithCorrectInstallments()
    {
        var company = MakeCompany();
        var settings = MakeSettings(taxEnabled: false);
        var saleId = Guid.NewGuid();

        _companyRepo.Setup(r => r.GetByTenantIdAsync(_companyId.ToString())).ReturnsAsync(company);
        _companyRepo.Setup(r => r.GetSettingsAsync(company.Id)).ReturnsAsync(settings);

        var request = new CreateCreditSaleRequest(
            _clientId, _employeeId, 0, null, _branchId,
            new List<SaleDetailItem>
            {
                new(Guid.NewGuid(), "Product A", 1, 3000m, 0, 3000m),
            },
            500m, 3, 5m);

        var sale = MakeSale(saleId);
        sale.SaleType = "credit";
        _mapper.Setup(m => m.Map<Sale>(request)).Returns(sale);
        _saleRepo.Setup(r => r.GenerateInvoiceNumberAsync(company.Id)).ReturnsAsync("INV-002");

        Sale? capturedSale = null;
        _saleRepo.Setup(r => r.AddAsync(It.IsAny<Sale>()))
            .Callback<Sale>(s => capturedSale = s);

        var expectedResponse = new SaleResponse(
            saleId, "INV-002", _clientId, "Client", _employeeId, "Employee",
            sale.SaleDate, "credit", 3000m, 0, 0, 3000m, 500m, 2500m, "pending", null,
            new List<SaleDetailItem>(), Guid.NewGuid());

        _saleRepo.Setup(r => r.GetByIdAsync(saleId)).ReturnsAsync(sale);
        _mapper.Setup(m => m.Map<SaleResponse>(sale)).Returns(expectedResponse);

        var product = new Product { Id = Guid.NewGuid(), Stock = 10, CostPrice = 2000m };
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var result = await _sut.CreateCreditSaleAsync(request);

        result.Should().NotBeNull();
        capturedSale.Should().NotBeNull();
        capturedSale!.Credit.Should().NotBeNull();
        capturedSale.Credit.InstallmentCount.Should().Be(3);
        capturedSale.Credit.FinancedAmount.Should().Be(2500m);
        capturedSale.Credit.Installments.Should().HaveCount(3);
        _saleRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSaleNotFound()
    {
        var id = Guid.NewGuid();
        _saleRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Sale?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto_WhenSaleFound()
    {
        var saleId = Guid.NewGuid();
        var sale = MakeSale(saleId);
        _saleRepo.Setup(r => r.GetByIdAsync(saleId)).ReturnsAsync(sale);

        var expected = new SaleResponse(
            saleId, "INV-001", _clientId, "Client", _employeeId, "Employee",
            sale.SaleDate, "cash", 1000m, 160m, 0, 1160m, 1160m, 0, "completed", null,
            new List<SaleDetailItem>(), null);

        _mapper.Setup(m => m.Map<SaleResponse>(sale)).Returns(expected);

        var result = await _sut.GetByIdAsync(saleId);

        result.Should().NotBeNull();
        result.Should().Be(expected);
    }
}
