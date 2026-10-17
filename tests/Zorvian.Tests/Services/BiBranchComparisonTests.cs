using FluentAssertions;
using Moq;
using Xunit;
using Zorvian.Application.DTOs.Bi;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

/// <summary>
/// Fase C: comparativo por sucursal (vista "empresa"). El servicio mapea las filas
/// 1:1 del SQL crudo y calcula los totales de la compañía — NO lee BranchScope
/// (a propósito: este endpoint compara todas las sucursales entre sí).
/// </summary>
public sealed class BiBranchComparisonTests
{
    private readonly Mock<IDashboardRepository> _dashRepo = new();
    private readonly BiService _sut;

    public BiBranchComparisonTests()
    {
        _sut = new BiService(
            Mock.Of<ISaleRepository>(), Mock.Of<IPurchaseRepository>(),
            Mock.Of<ICreditRepository>(), Mock.Of<IProductRepository>(),
            Mock.Of<ICashRegisterRepository>(), Mock.Of<IAccountRepository>(),
            Mock.Of<IAccountingEntryRepository>(), Mock.Of<IAccountingPeriodRepository>(),
            Mock.Of<IClientRepository>(), Mock.Of<ISupplierRepository>(),
            Mock.Of<IQuoteRepository>(), _dashRepo.Object,
            Mock.Of<ICompanyRepository>(), Mock.Of<ITenantContext>());
    }

    [Fact]
    public async Task GetBranchComparisonAsync_MapsRowsAndSumsCompanyTotals()
    {
        _dashRepo.Setup(r => r.GetBranchComparisonRawAsync(It.IsAny<Guid?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<BranchComparisonScalars>
            {
                new() { BranchId = Guid.NewGuid(), BranchName = "Central", MonthSales = 100, SalesCount = 2, MonthRecovery = 40, Portfolio = 500, ActiveCredits = 5, OverdueCredits = 1, InventoryValue = 1000 },
                new() { BranchId = Guid.NewGuid(), BranchName = "Norte", MonthSales = 50, SalesCount = 1, MonthRecovery = 10, Portfolio = 200, ActiveCredits = 3, OverdueCredits = 2, InventoryValue = 300 },
            });

        var result = await _sut.GetBranchComparisonAsync();

        result.Branches.Should().HaveCount(2);
        result.Branches[0].BranchName.Should().Be("Central");
        result.Branches[0].MonthSales.Should().Be(100);
        result.Branches[1].BranchName.Should().Be("Norte");
        result.TotalMonthSales.Should().Be(150);
        result.TotalSalesCount.Should().Be(3);
        result.TotalMonthRecovery.Should().Be(50);
        result.TotalPortfolio.Should().Be(700);
        result.TotalActiveCredits.Should().Be(8);
        result.TotalOverdueCredits.Should().Be(3);
        result.TotalInventoryValue.Should().Be(1300);
    }

    [Fact]
    public async Task GetBranchComparisonAsync_NoBranches_ReturnsEmptyWithZeroTotals()
    {
        _dashRepo.Setup(r => r.GetBranchComparisonRawAsync(It.IsAny<Guid?>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<BranchComparisonScalars>());

        var result = await _sut.GetBranchComparisonAsync();

        result.Branches.Should().BeEmpty();
        result.TotalMonthSales.Should().Be(0);
        result.TotalPortfolio.Should().Be(0);
        result.TotalInventoryValue.Should().Be(0);
    }
}
