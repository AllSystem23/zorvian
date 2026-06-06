using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Core.Enums;
using FluentAssertions;

namespace Zorvian.Tests.Services;

public sealed class WarrantyProfitabilityReportServiceTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly string _tenantId;
    private readonly Guid _companyId;
    private readonly ZorvianDbContext _db;
    private readonly WarrantyCostRepository _costRepo;
    private readonly WarrantyRepository _warrantyRepo;

    public WarrantyProfitabilityReportServiceTests()
    {
        _companyId = Guid.NewGuid();
        _tenantId = _companyId.ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _costRepo = new WarrantyCostRepository(_db);
        _warrantyRepo = new WarrantyRepository(_db);
    }

    [Fact]
    public async Task GetCostByWarrantyAsync_ReturnsZero_WhenNoCosts()
    {
        var service = new WarrantyProfitabilityReportService(_costRepo, _warrantyRepo, _tenant.Object);
        var result = await service.GetCostByWarrantyAsync(Guid.NewGuid());
        result.TotalCost.Should().Be(0);
        result.PartsCost.Should().Be(0);
        result.LaborCost.Should().Be(0);
        result.OtherCost.Should().Be(0);
    }

[Fact]
public async Task GetCostByWarrantyAsync_ReturnsCorrectBreakdown()
{
    var warrantyId = Guid.NewGuid();
    _db.Set<WarrantyCost>().AddRange(
        new WarrantyCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarrantyId = warrantyId,
            CostCategory = "parts",
            Quantity = 2,
            UnitCost = 100m,
            PaidBy = "company",
            IsBilled = true,
            CompanyId = _companyId,
            RegisteredAt = DateTime.UtcNow
        },
        new WarrantyCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarrantyId = warrantyId,
            CostCategory = "labor",
            Quantity = 3,
            UnitCost = 50m,
            PaidBy = "company",
            IsBilled = true,
            CompanyId = _companyId,
            RegisteredAt = DateTime.UtcNow
        },
        new WarrantyCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarrantyId = warrantyId,
            CostCategory = "shipping",
            Quantity = 1,
            UnitCost = 25m,
            PaidBy = "company",
            IsBilled = true,
            CompanyId = _companyId,
            RegisteredAt = DateTime.UtcNow
        }
    );
    await _db.SaveChangesAsync();

        var service = new WarrantyProfitabilityReportService(_costRepo, _warrantyRepo, _tenant.Object);
        var result = await service.GetCostByWarrantyAsync(warrantyId);

        result.TotalCost.Should().Be(375m);
        result.PartsCost.Should().Be(200m);
        result.LaborCost.Should().Be(150m);
        result.OtherCost.Should().Be(25m);
        result.BilledCostCount.Should().Be(3);
    }

    [Fact]
    public async Task GetCostByWarrantyAsync_IgnoresUnbilledCosts()
    {
        var warrantyId = Guid.NewGuid();
        _db.Set<WarrantyCost>().AddRange(
            new WarrantyCost
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                WarrantyId = warrantyId,
                CostCategory = "parts",
                Quantity = 1,
                UnitCost = 500m,
                PaidBy = "company",
                IsBilled = false,
                CompanyId = _companyId,
                RegisteredAt = DateTime.UtcNow
            }
        );
        await _db.SaveChangesAsync();

        var service = new WarrantyProfitabilityReportService(_costRepo, _warrantyRepo, _tenant.Object);
        var result = await service.GetCostByWarrantyAsync(warrantyId);

        result.TotalCost.Should().Be(0);
    }

    [Fact]
    public async Task GetProfitabilityReportAsync_ReturnsReportWithCorrectMetrics()
    {
        var brandId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();

        _db.Set<Brand>().Add(new Brand { Id = brandId, Name = "TestBrand", TenantId = _tenantId, CompanyId = _companyId });
        _db.Set<Product>().Add(new Product
        {
            Id = productId,
            TenantId = _tenantId,
            Name = "TestProduct",
            BrandId = brandId,
            CostPrice = 100m,
            SellingPrice = 300m,
            CompanyId = _companyId
        });
        _db.Set<Sale>().Add(new Sale
        {
            Id = saleId,
            TenantId = _tenantId,
            Total = 1000m,
            SaleDate = DateTime.UtcNow,
            CompanyId = _companyId,
            BranchId = Guid.NewGuid()
        });
        _db.Set<Warranty>().Add(new Warranty
        {
            Id = warrantyId,
            TenantId = _tenantId,
            ProductId = productId,
            SaleId = saleId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            CompanyId = _companyId,
            BranchId = Guid.NewGuid(),
            Status = WarrantyStatus.Registered,
            WarrantyNumber = "W-001"
        });
        _db.Set<WarrantyCost>().Add(new WarrantyCost
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            WarrantyId = warrantyId,
            CostCategory = "parts",
            Quantity = 2,
            UnitCost = 150m,
            PaidBy = "company",
            IsBilled = true,
            CompanyId = _companyId,
            RegisteredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var service = new WarrantyProfitabilityReportService(_costRepo, _warrantyRepo, _tenant.Object);
        var result = await service.GetProfitabilityReportAsync(
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow.AddMonths(1));

        result.TotalWarrantyCost.Should().Be(300m);
        result.TotalSaleValue.Should().Be(1000m);
        result.ByBrand.Should().HaveCount(1);
        result.ByBrand[0].BrandName.Should().Be("TestBrand");
        result.ByBrand[0].TotalWarrantyCost.Should().Be(300m);
        result.ByBrand[0].TotalSaleValue.Should().Be(1000m);
        result.CostBreakdown.Should().Contain(b => b.Category == "parts" && b.Total == 300m);
        result.MonthlyTrend.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProfitabilityReportAsync_ReturnsEmpty_WhenNoData()
    {
        var service = new WarrantyProfitabilityReportService(_costRepo, _warrantyRepo, _tenant.Object);
        var result = await service.GetProfitabilityReportAsync(
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow.AddMonths(1));

        result.TotalWarrantyCost.Should().Be(0);
        result.TotalSaleValue.Should().Be(0);
        result.ByBrand.Should().BeEmpty();
        result.CostBreakdown.Should().BeEmpty();
        result.MonthlyTrend.Should().BeEmpty();
    }
}
