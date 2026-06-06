using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

public sealed class WarrantyProviderRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly WarrantyProviderRepository _sut;

    public WarrantyProviderRepositoryTests()
    {
        var tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new WarrantyProviderRepository(_db);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllForCompany()
    {
        var companyId = Guid.NewGuid();
        _db.WarrantyProviders.AddRange(
            new WarrantyProvider { Id = Guid.NewGuid(), Code = "PV001", Name = "Provider A", Type = "manufacturer", CompanyId = companyId, TenantId = _tenant.Object.TenantId.ToString(), CreatedBy = "test" },
            new WarrantyProvider { Id = Guid.NewGuid(), Code = "PV002", Name = "Provider B", Type = "distributor", CompanyId = companyId, TenantId = _tenant.Object.TenantId.ToString(), CreatedBy = "test" }
        );
        await _db.SaveChangesAsync();

        var results = await _sut.GetAllAsync(companyId);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_PersistsProvider()
    {
        var provider = new WarrantyProvider { Id = Guid.NewGuid(), Code = "PV003", Name = "Provider C", Type = "supplier", CompanyId = Guid.NewGuid(), TenantId = _tenant.Object.TenantId.ToString(), CreatedBy = "test" };

        await _sut.AddAsync(provider);
        await _sut.SaveChangesAsync();

        var saved = await _db.WarrantyProviders.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == provider.Id);
        Assert.NotNull(saved);
    }
}
