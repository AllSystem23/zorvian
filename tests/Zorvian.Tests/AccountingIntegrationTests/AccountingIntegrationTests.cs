using Moq;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Application.Interfaces;

namespace Zorvian.Tests.AccountingIntegrationTests;

public class AccountingIntegrationTests
{
    [Fact]
    public async Task Sale_WithMultipleTaxRates_ShouldGenerateSegregatedAccountingEntries()
    {
        // ARRANGE
        var mockProductRepo = new Mock<IProductRepository>();
        var mockSaleRepo = new Mock<ISaleRepository>();
        var mockMovementRepo = new Mock<IInventoryMovementRepository>();
        var mockCompanyRepo = new Mock<ICompanyRepository>();
        var mockTenant = new Mock<ITenantContext>();
        var mockMapper = new Mock<AutoMapper.IMapper>();

        var companyId = Guid.NewGuid();
        mockTenant.Setup(t => t.TenantId).Returns(companyId.ToString());

        var mockAutoAccounting = new Mock<IAutoAccountingService>();
        
        // Setup SaleService
        var saleService = new SaleService(
            mockSaleRepo.Object, mockProductRepo.Object, mockMovementRepo.Object,
            mockCompanyRepo.Object, mockAutoAccounting.Object, mockTenant.Object, mockMapper.Object);

        // Mock two products with different tax categories
        var prod1 = new Product { Id = Guid.NewGuid(), TaxCategory = new TaxCategory { Rate = 0.15m, Name = "15%" }, CostPrice = 10 };
        var prod2 = new Product { Id = Guid.NewGuid(), TaxCategory = new TaxCategory { Rate = 0.10m, Name = "10%" }, CostPrice = 20 };
        
        mockProductRepo.Setup(r => r.GetByIdAsync(prod1.Id)).ReturnsAsync(prod1);
        mockProductRepo.Setup(r => r.GetByIdAsync(prod2.Id)).ReturnsAsync(prod2);
        
        mockSaleRepo.Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<Guid>())).ReturnsAsync("INV-001");

        // ACT - Venta de ambos
        var request = new CreateCashSaleRequest(
            Guid.NewGuid(), 
            Guid.NewGuid(), // EmployeeId
            0, // Discount
            "Notes", 
            Guid.NewGuid(), // BranchId
            new List<SaleDetailItem> { 
                new(prod1.Id, "P1", 10, 100, 0, 1000), 
                new(prod2.Id, "P2", 10, 100, 0, 1000) 
            },
            new SalePaymentInfo(2300, "cash", "REF1", Guid.NewGuid())
        );

        var sale = new Sale { Id = Guid.NewGuid(), InvoiceNumber = "INV-001" };
        mockMapper.Setup(m => m.Map<Sale>(It.IsAny<CreateCashSaleRequest>())).Returns(sale);
        mockSaleRepo.Setup(r => r.GenerateInvoiceNumberAsync(It.IsAny<Guid>())).ReturnsAsync("INV-001");
        mockSaleRepo.Setup(r => r.GetByIdAsync(sale.Id)).ReturnsAsync(sale);
        mockMapper.Setup(m => m.Map<SaleResponse>(sale)).Returns(new SaleResponse(sale.Id, "INV-001", Guid.NewGuid(), "Client", Guid.NewGuid(), "Emp", DateTime.UtcNow, "cash", 0, 0, 0, 0, 0, 0, "ok", null, new List<SaleDetailItem>(), null));

        await saleService.CreateCashSaleAsync(request);

        // ASSERT: Verificar llamada al servicio contable con detalles
        mockAutoAccounting.Verify(a => a.GenerateSaleEntryAsync(
            It.IsAny<Guid>(), 
            It.Is<List<SaleDetail>>(d => d.Count == 2), 
            0, 2300, "cash"), 
            Times.Once);
    }
}
