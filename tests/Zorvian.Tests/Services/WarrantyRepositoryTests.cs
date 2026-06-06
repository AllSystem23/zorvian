using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

public sealed class WarrantyRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly WarrantyRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _otherBranchId = Guid.NewGuid();
    private readonly Guid _brandId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public WarrantyRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new WarrantyRepository(_db);
    }

    private Warranty MakeWarranty(Guid? branchId = null, WarrantyStatus? status = null, Guid? clientId = null)
    {
        var id = Guid.NewGuid();
        var cId = clientId ?? Guid.NewGuid();
        var pId = Guid.NewGuid();

        _db.Clients.Add(MakeClient(cId));
        _db.Products.Add(MakeProduct(pId));

        return new Warranty
        {
            Id = id,
            TenantId = _tenantId,
            BranchId = branchId ?? _branchId,
            CompanyId = _companyId,
            ClientId = cId,
            ProductId = pId,
            BrandId = null,
            CategoryId = null,
            WarrantyNumber = $"GAR-{id.ToString()[..8]}",
            Status = status ?? WarrantyStatus.Registered,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11)),
            DurationMonths = 12,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            Claims = []
        };
    }

    private Client MakeClient(Guid clientId)
    {
        return new Client
        {
            Id = clientId,
            TenantId = _tenantId,
            Code = $"C{clientId.ToString()[..6]}",
            FirstName = "Test",
            LastName = "Client",
            CompanyId = _companyId,
            BranchId = _branchId,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private Product MakeProduct(Guid productId)
    {
        return new Product
        {
            Id = productId,
            TenantId = _tenantId,
            Code = $"P{productId.ToString()[..6]}",
            Name = "Test Product",
            CostPrice = 100m,
            SellingPrice = 150m,
            Stock = 10,
            MinStock = 1,
            MaxStock = 100,
            UnitOfMeasure = "unit",
            IsActive = true,
            CompanyId = _companyId,
            BranchId = _branchId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private Brand MakeBrand(Guid brandId)
    {
        return new Brand
        {
            Id = brandId,
            TenantId = _tenantId,
            Name = "Test Brand",
            CompanyId = _companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    private Category MakeCategory(Guid categoryId)
    {
        return new Category
        {
            Id = categoryId,
            TenantId = _tenantId,
            Name = "Test Category",
            CompanyId = _companyId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    [Fact]
    public async Task GetFilteredAsync_WithGuidEmpty_DoesNotFilterByBranch()
    {
        _db.Warranties.AddRange(
            MakeWarranty(branchId: _branchId),
            MakeWarranty(branchId: _otherBranchId)
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetFilteredAsync(null, null, null, Guid.Empty, 1, 50);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_WithSpecificBranch_FiltersByBranch()
    {
        _db.Warranties.AddRange(
            MakeWarranty(branchId: _branchId),
            MakeWarranty(branchId: _otherBranchId)
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetFilteredAsync(null, null, null, _branchId, 1, 50);

        Assert.Single(results);
        Assert.Equal(_branchId, results[0].BranchId);
    }

    [Fact]
    public async Task GetFilteredAsync_WithClientId_FiltersByClient()
    {
        var targetClient = Guid.NewGuid();
        var w1 = MakeWarranty(branchId: _branchId, clientId: targetClient);

        _db.Warranties.AddRange(
            w1,
            MakeWarranty(branchId: _branchId)
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetFilteredAsync(targetClient, null, null, Guid.Empty, 1, 50);

        Assert.Single(results);
    }

    [Fact]
    public async Task GetFilteredCountAsync_WithGuidEmpty_CountsAllBranches()
    {
        _db.Warranties.AddRange(
            MakeWarranty(branchId: _branchId),
            MakeWarranty(branchId: _otherBranchId)
        );
        await _db.SaveChangesAsync();

        var count = await _sut.GetFilteredCountAsync(null, null, null, Guid.Empty);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetFilteredCountAsync_WithSpecificBranch_CountsOnlyThatBranch()
    {
        _db.Warranties.AddRange(
            MakeWarranty(branchId: _branchId),
            MakeWarranty(branchId: _otherBranchId)
        );
        await _db.SaveChangesAsync();

        var count = await _sut.GetFilteredCountAsync(null, null, null, _branchId);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetFilteredCountAsync_WithStatus_FiltersByStatus()
    {
        _db.Warranties.AddRange(
            MakeWarranty(status: WarrantyStatus.Registered),
            MakeWarranty(status: WarrantyStatus.InDiagnosis)
        );
        await _db.SaveChangesAsync();

        var count = await _sut.GetFilteredCountAsync(null, "Registered", null, Guid.Empty);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsWarrantyWithIncludes()
    {
        var w = MakeWarranty(branchId: _branchId);
        w.BrandId = _brandId;
        w.CategoryId = _categoryId;
        _db.Brands.Add(MakeBrand(_brandId));
        _db.Categories.Add(MakeCategory(_categoryId));
        _db.Warranties.Add(w);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(w.Id);

        Assert.NotNull(result);
        Assert.Equal(w.Id, result.Id);
    }

    [Fact]
    public async Task AddAsync_PersistsWarranty()
    {
        var w = MakeWarranty();

        await _sut.AddAsync(w);
        await _sut.SaveChangesAsync();

        var saved = await _db.Warranties.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == w.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingWarranty()
    {
        var w = MakeWarranty();
        _db.Warranties.Add(w);
        await _db.SaveChangesAsync();
        _db.Entry(w).State = EntityState.Detached;

        w.Status = WarrantyStatus.InDiagnosis;

        await _sut.UpdateAsync(w);
        await _sut.SaveChangesAsync();

        var updated = await _db.Warranties.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == w.Id);
        Assert.NotNull(updated);
    }
}
